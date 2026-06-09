using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace RVO.JobSystem
{
    [BurstCompile]
    internal struct ComputeAgentVelocityJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<JobAgentData> Agents;
        [ReadOnly] public NativeArray<JobObstacleData> Obstacles;
        [ReadOnly] public NativeArray<int> NeighborIndices;
        [ReadOnly] public NativeArray<int> NeighborCounts;
        [ReadOnly] public NativeArray<int> ObstacleNeighborIndices;

        [ReadOnly] public NativeArray<int> ObstacleNeighborCounts;

        // 一个通过 agentType - agentType 查找额外半径的矩阵，长度为 agentTypeCount * agentTypeCount
        // 索引方式 agentTypeA * agentTypeCount + agentTypeB
        [ReadOnly] public NativeArray<float> ExtraRadii;
        public int AgentTypeCount;

        [NativeDisableParallelForRestriction] public NativeArray<JobLine> OrcaLines;
        [NativeDisableParallelForRestriction] public NativeArray<JobLine> TempOrcaLines;
        [NativeDisableParallelForRestriction] public NativeArray<int> OrcaLineCounts;
        [NativeDisableParallelForRestriction] public NativeArray<int> ObstacleOrcaLineCounts;
        [NativeDisableParallelForRestriction] public NativeArray<JobAgentOutput> Outputs;
        public int MaxNeighborCapacity;
        public int MaxObstacleNeighborCapacity;
        public int MaxOrcaLineCapacity;
        public float TimeStep;

        public void Execute(int index)
        {
            var agent = Agents[index];
            var lineStart = index * MaxOrcaLineCapacity;
            var neighborStart = index * MaxNeighborCapacity;
            var obstacleNeighborStart = index * MaxObstacleNeighborCapacity;
            var neighborCount = NeighborCounts[index];
            var obstacleNeighborCount = ObstacleNeighborCounts[index];
            var invTimeHorizonObst = 1.0f / math.max(agent.timeHorizonObst, MathUtil.RvoEpsilon);
            var invTimeHorizon = 1.0f / math.max(agent.timeHorizon, MathUtil.RvoEpsilon);
            var lineCount = 0;

            for (var slot = 0; slot < obstacleNeighborCount; slot++)
            {
                var obstacleIndex = ObstacleNeighborIndices[obstacleNeighborStart + slot];
                if (obstacleIndex < 0 || obstacleIndex >= Obstacles.Length)
                {
                    continue;
                }

                var obstacle1 = Obstacles[obstacleIndex];
                var obstacle2 = Obstacles[obstacle1.next];

                var relativePosition1 = obstacle1.point - agent.position;
                var relativePosition2 = obstacle2.point - agent.position;
                var alreadyCovered = false;

                for (var existing = 0; existing < lineCount; existing++)
                {
                    var line = OrcaLines[lineStart + existing];
                    var left = MathUtil.Det(invTimeHorizonObst * relativePosition1 - line.point, line.direction) -
                               invTimeHorizonObst * agent.radius;
                    var right = MathUtil.Det(invTimeHorizonObst * relativePosition2 - line.point, line.direction) -
                                invTimeHorizonObst * agent.radius;
                    if (left >= -MathUtil.RvoEpsilon && right >= -MathUtil.RvoEpsilon)
                    {
                        alreadyCovered = true;
                        break;
                    }
                }

                if (alreadyCovered)
                {
                    continue;
                }

                var distSq1 = MathUtil.AbsSq(relativePosition1);
                var distSq2 = MathUtil.AbsSq(relativePosition2);
                var radiusSq = agent.radius * agent.radius;

                var obstacleVector = obstacle2.point - obstacle1.point;
                var obstacleVectorAbsSq = math.max(MathUtil.AbsSq(obstacleVector), MathUtil.RvoEpsilon);
                var s = math.dot(-relativePosition1, obstacleVector) / obstacleVectorAbsSq;
                var distSqLine = MathUtil.AbsSq(-relativePosition1 - s * obstacleVector);

                JobLine constraint;

                if (s < 0.0f && distSq1 <= radiusSq)
                {
                    if (obstacle1.convex == 1)
                    {
                        constraint.point = float3.zero;
                        constraint.direction =
                            MathUtil.NormalizeSafe(new float3(-relativePosition1.y, relativePosition1.x, 0.0f));
                        if (lineCount < MaxOrcaLineCapacity)
                        {
                            OrcaLines[lineStart + lineCount] = constraint;
                            lineCount++;
                        }
                    }

                    continue;
                }

                if (s > 1.0f && distSq2 <= radiusSq)
                {
                    if (obstacle2.convex == 1 && MathUtil.Det(relativePosition2, obstacle2.direction) >= 0.0f)
                    {
                        constraint.point = float3.zero;
                        constraint.direction =
                            MathUtil.NormalizeSafe(new float3(-relativePosition2.y, relativePosition2.x, 0.0f));
                        if (lineCount < MaxOrcaLineCapacity)
                        {
                            OrcaLines[lineStart + lineCount] = constraint;
                            lineCount++;
                        }
                    }

                    continue;
                }

                if (s >= 0.0f && s < 1.0f && distSqLine <= radiusSq)
                {
                    constraint.point = float3.zero;
                    constraint.direction = -obstacle1.direction;
                    if (lineCount < MaxOrcaLineCapacity)
                    {
                        OrcaLines[lineStart + lineCount] = constraint;
                        lineCount++;
                    }

                    continue;
                }

                float3 leftLegDirection;
                float3 rightLegDirection;
                var leftObstacle = obstacle1;
                var rightObstacle = obstacle2;

                if (s < 0.0f && distSqLine <= radiusSq)
                {
                    if (obstacle1.convex == 0)
                    {
                        continue;
                    }

                    rightObstacle = obstacle1;
                    var leg1 = math.sqrt(math.max(0.0f, distSq1 - radiusSq));
                    leftLegDirection = new float3(
                        relativePosition1.x * leg1 - relativePosition1.y * agent.radius,
                        relativePosition1.x * agent.radius + relativePosition1.y * leg1,
                        0.0f) / math.max(distSq1, MathUtil.RvoEpsilon);
                    rightLegDirection = new float3(
                        relativePosition1.x * leg1 + relativePosition1.y * agent.radius,
                        -relativePosition1.x * agent.radius + relativePosition1.y * leg1,
                        0.0f) / math.max(distSq1, MathUtil.RvoEpsilon);
                }
                else if (s > 1.0f && distSqLine <= radiusSq)
                {
                    if (obstacle2.convex == 0)
                    {
                        continue;
                    }

                    leftObstacle = obstacle2;
                    var leg2 = math.sqrt(math.max(0.0f, distSq2 - radiusSq));
                    leftLegDirection = new float3(
                        relativePosition2.x * leg2 - relativePosition2.y * agent.radius,
                        relativePosition2.x * agent.radius + relativePosition2.y * leg2,
                        0.0f) / math.max(distSq2, MathUtil.RvoEpsilon);
                    rightLegDirection = new float3(
                        relativePosition2.x * leg2 + relativePosition2.y * agent.radius,
                        -relativePosition2.x * agent.radius + relativePosition2.y * leg2,
                        0.0f) / math.max(distSq2, MathUtil.RvoEpsilon);
                }
                else
                {
                    if (obstacle1.convex == 1)
                    {
                        var leg1 = math.sqrt(math.max(0.0f, distSq1 - radiusSq));
                        leftLegDirection = new float3(
                            relativePosition1.x * leg1 - relativePosition1.y * agent.radius,
                            relativePosition1.x * agent.radius + relativePosition1.y * leg1,
                            0.0f) / math.max(distSq1, MathUtil.RvoEpsilon);
                    }
                    else
                    {
                        leftLegDirection = -obstacle1.direction;
                    }

                    if (obstacle2.convex == 1)
                    {
                        var leg2 = math.sqrt(math.max(0.0f, distSq2 - radiusSq));
                        rightLegDirection = new float3(
                            relativePosition2.x * leg2 + relativePosition2.y * agent.radius,
                            -relativePosition2.x * agent.radius + relativePosition2.y * leg2,
                            0.0f) / math.max(distSq2, MathUtil.RvoEpsilon);
                    }
                    else
                    {
                        rightLegDirection = obstacle1.direction;
                    }
                }

                var leftNeighbor = Obstacles[leftObstacle.previous];
                var isLeftLegForeign = false;
                var isRightLegForeign = false;

                if (leftObstacle.convex == 1 && MathUtil.Det(leftLegDirection, -leftNeighbor.direction) >= 0.0f)
                {
                    leftLegDirection = -leftNeighbor.direction;
                    isLeftLegForeign = true;
                }

                if (rightObstacle.convex == 1 && MathUtil.Det(rightLegDirection, rightObstacle.direction) <= 0.0f)
                {
                    rightLegDirection = rightObstacle.direction;
                    isRightLegForeign = true;
                }

                var leftCutOff = invTimeHorizonObst * (leftObstacle.point - agent.position);
                var rightCutOff = invTimeHorizonObst * (rightObstacle.point - agent.position);
                var cutOffVector = rightCutOff - leftCutOff;
                var cutOffAbsSq = math.max(MathUtil.AbsSq(cutOffVector), MathUtil.RvoEpsilon);

                var t = leftObstacle.id == rightObstacle.id
                    ? 0.5f
                    : math.dot(agent.velocity - leftCutOff, cutOffVector) / cutOffAbsSq;
                var tLeft = math.dot(agent.velocity - leftCutOff, leftLegDirection);
                var tRight = math.dot(agent.velocity - rightCutOff, rightLegDirection);

                if ((t < 0.0f && tLeft < 0.0f) ||
                    (leftObstacle.id == rightObstacle.id && tLeft < 0.0f && tRight < 0.0f))
                {
                    var unitW = MathUtil.NormalizeSafe(agent.velocity - leftCutOff);
                    constraint.direction = new float3(unitW.y, -unitW.x, 0.0f);
                    constraint.point = leftCutOff + agent.radius * invTimeHorizonObst * unitW;
                    if (lineCount < MaxOrcaLineCapacity)
                    {
                        OrcaLines[lineStart + lineCount] = constraint;
                        lineCount++;
                    }

                    continue;
                }

                if (t > 1.0f && tRight < 0.0f)
                {
                    var unitW = MathUtil.NormalizeSafe(agent.velocity - rightCutOff);
                    constraint.direction = new float3(unitW.y, -unitW.x, 0.0f);
                    constraint.point = rightCutOff + agent.radius * invTimeHorizonObst * unitW;
                    if (lineCount < MaxOrcaLineCapacity)
                    {
                        OrcaLines[lineStart + lineCount] = constraint;
                        lineCount++;
                    }

                    continue;
                }

                var distSqCutoff = (t < 0.0f || t > 1.0f || leftObstacle.id == rightObstacle.id)
                    ? float.PositiveInfinity
                    : MathUtil.AbsSq(agent.velocity - (leftCutOff + t * cutOffVector));
                var distSqLeft = tLeft < 0.0f
                    ? float.PositiveInfinity
                    : MathUtil.AbsSq(agent.velocity - (leftCutOff + tLeft * leftLegDirection));
                var distSqRight = tRight < 0.0f
                    ? float.PositiveInfinity
                    : MathUtil.AbsSq(agent.velocity - (rightCutOff + tRight * rightLegDirection));

                if (distSqCutoff <= distSqLeft && distSqCutoff <= distSqRight)
                {
                    constraint.direction = -obstacle1.direction;
                    constraint.point = leftCutOff + agent.radius * invTimeHorizonObst *
                        new float3(-constraint.direction.y, constraint.direction.x, 0.0f);
                    if (lineCount < MaxOrcaLineCapacity)
                    {
                        OrcaLines[lineStart + lineCount] = constraint;
                        lineCount++;
                    }

                    continue;
                }

                if (distSqLeft <= distSqRight)
                {
                    if (isLeftLegForeign)
                    {
                        continue;
                    }

                    constraint.direction = leftLegDirection;
                    constraint.point = leftCutOff + agent.radius * invTimeHorizonObst *
                        new float3(-constraint.direction.y, constraint.direction.x, 0.0f);
                    if (lineCount < MaxOrcaLineCapacity)
                    {
                        OrcaLines[lineStart + lineCount] = constraint;
                        lineCount++;
                    }

                    continue;
                }

                if (isRightLegForeign)
                {
                    continue;
                }

                constraint.direction = -rightLegDirection;
                constraint.point = rightCutOff + agent.radius * invTimeHorizonObst *
                    new float3(-constraint.direction.y, constraint.direction.x, 0.0f);
                if (lineCount < MaxOrcaLineCapacity)
                {
                    OrcaLines[lineStart + lineCount] = constraint;
                    lineCount++;
                }
            }

            ObstacleOrcaLineCounts[index] = lineCount;

            for (var slot = 0; slot < neighborCount; slot++)
            {
                var otherIndex = NeighborIndices[neighborStart + slot];
                if (otherIndex < 0)
                {
                    continue;
                }

                var other = Agents[otherIndex];
                var relativePosition = other.position - agent.position;
                var relativeVelocity = agent.velocity - other.velocity;
                var distSq = MathUtil.AbsSq(relativePosition);
                var extraRadius = ExtraRadii[agent.agentType * AgentTypeCount + other.agentType];
                var combinedRadius = agent.radius + other.radius + extraRadius;
                var combinedRadiusSq = combinedRadius * combinedRadius;

                JobLine line;
                float3 u;

                if (distSq > combinedRadiusSq)
                {
                    var w = relativeVelocity - invTimeHorizon * relativePosition;
                    var wLengthSq = MathUtil.AbsSq(w);
                    var dotProduct1 = math.dot(w, relativePosition);

                    if (dotProduct1 < 0.0f && dotProduct1 * dotProduct1 > combinedRadiusSq * wLengthSq)
                    {
                        var wLength = math.sqrt(wLengthSq);
                        var unitW = wLength > MathUtil.RvoEpsilon ? w / wLength : float3.zero;

                        line.direction = new float3(unitW.y, -unitW.x, 0.0f);
                        u = (combinedRadius * invTimeHorizon - wLength) * unitW;
                    }
                    else
                    {
                        var leg = math.sqrt(math.max(0.0f, distSq - combinedRadiusSq));

                        if (MathUtil.Det(relativePosition, w) > 0.0f)
                        {
                            line.direction = new float3(
                                relativePosition.x * leg - relativePosition.y * combinedRadius,
                                relativePosition.x * combinedRadius + relativePosition.y * leg,
                                0.0f) / math.max(distSq, MathUtil.RvoEpsilon);
                        }
                        else
                        {
                            line.direction = -new float3(
                                relativePosition.x * leg + relativePosition.y * combinedRadius,
                                -relativePosition.x * combinedRadius + relativePosition.y * leg,
                                0.0f) / math.max(distSq, MathUtil.RvoEpsilon);
                        }

                        var dotProduct2 = math.dot(relativeVelocity, line.direction);
                        u = dotProduct2 * line.direction - relativeVelocity;
                    }
                }
                else
                {
                    var invTimeStep = 1.0f / math.max(TimeStep, MathUtil.RvoEpsilon);
                    var w = relativeVelocity - invTimeStep * relativePosition;
                    var wLength = math.length(w);
                    var unitW = wLength > MathUtil.RvoEpsilon ? w / wLength : float3.zero;

                    line.direction = new float3(unitW.y, -unitW.x, 0.0f);
                    u = (combinedRadius * invTimeStep - wLength) * unitW;
                }

                line.point = agent.velocity + 0.5f * u;
                if (lineCount < MaxOrcaLineCapacity)
                {
                    OrcaLines[lineStart + lineCount] = line;
                    lineCount++;
                }
            }

            OrcaLineCounts[index] = lineCount;

            var newVelocity = agent.prefVelocity;
            var lineFail = LinearProgram2(OrcaLines, lineStart, lineCount, agent.maxSpeed, agent.prefVelocity, false,
                ref newVelocity);
            if (lineFail < lineCount)
            {
                LinearProgram3(
                    OrcaLines,
                    TempOrcaLines,
                    lineStart,
                    lineStart,
                    lineCount,
                    ObstacleOrcaLineCounts[index],
                    lineFail,
                    agent.maxSpeed,
                    ref newVelocity);
            }

            Outputs[index] = new JobAgentOutput
            {
                newVelocity = newVelocity,
            };
        }

        private static bool LinearProgram1(NativeArray<JobLine> lines, int lineStart, int lineNo, float radius,
            float3 optVelocity, bool directionOpt, ref float3 result)
        {
            var line = lines[lineStart + lineNo];
            var dotProduct = math.dot(line.point, line.direction);
            var discriminant = dotProduct * dotProduct + radius * radius - MathUtil.AbsSq(line.point);

            if (discriminant < 0.0f)
            {
                return false;
            }

            var sqrtDiscriminant = math.sqrt(discriminant);
            var tLeft = -dotProduct - sqrtDiscriminant;
            var tRight = -dotProduct + sqrtDiscriminant;

            for (var index = 0; index < lineNo; index++)
            {
                var otherLine = lines[lineStart + index];
                var denominator = MathUtil.Det(line.direction, otherLine.direction);
                var numerator = MathUtil.Det(otherLine.direction, line.point - otherLine.point);

                if (math.abs(denominator) <= MathUtil.RvoEpsilon)
                {
                    if (numerator < 0.0f)
                    {
                        return false;
                    }

                    continue;
                }

                var t = numerator / denominator;
                if (denominator >= 0.0f)
                {
                    tRight = math.min(tRight, t);
                }
                else
                {
                    tLeft = math.max(tLeft, t);
                }

                if (tLeft > tRight)
                {
                    return false;
                }
            }

            if (directionOpt)
            {
                result = math.dot(optVelocity, line.direction) > 0.0f
                    ? line.point + tRight * line.direction
                    : line.point + tLeft * line.direction;
                return true;
            }

            var projectedT = math.dot(line.direction, optVelocity - line.point);
            if (projectedT < tLeft)
            {
                result = line.point + tLeft * line.direction;
            }
            else if (projectedT > tRight)
            {
                result = line.point + tRight * line.direction;
            }
            else
            {
                result = line.point + projectedT * line.direction;
            }

            return true;
        }

        private static int LinearProgram2(NativeArray<JobLine> lines, int lineStart, int lineCount, float radius,
            float3 optVelocity, bool directionOpt, ref float3 result)
        {
            if (directionOpt)
            {
                result = optVelocity * radius;
            }
            else if (MathUtil.AbsSq(optVelocity) > radius * radius)
            {
                result = MathUtil.NormalizeSafe(optVelocity) * radius;
            }
            else
            {
                result = optVelocity;
            }

            for (var index = 0; index < lineCount; index++)
            {
                var line = lines[lineStart + index];
                if (!(MathUtil.Det(line.direction, line.point - result) > 0.0f))
                {
                    continue;
                }

                var tempResult = result;
                if (!LinearProgram1(lines, lineStart, index, radius, optVelocity, directionOpt, ref result))
                {
                    result = tempResult;
                    return index;
                }
            }

            return lineCount;
        }

        private static void LinearProgram3(NativeArray<JobLine> lines, NativeArray<JobLine> tempLines, int lineStart,
            int tempStart, int lineCount, int numObstacleLines, int beginLine, float radius, ref float3 result)
        {
            var distance = 0.0f;

            for (var index = beginLine; index < lineCount; index++)
            {
                var line = lines[lineStart + index];
                if (!(MathUtil.Det(line.direction, line.point - result) > distance))
                {
                    continue;
                }

                var projectionLineCount = 0;
                for (var obstacleIndex = 0; obstacleIndex < numObstacleLines; obstacleIndex++)
                {
                    tempLines[tempStart + projectionLineCount] = lines[lineStart + obstacleIndex];
                    projectionLineCount++;
                }

                for (var previous = numObstacleLines; previous < index; previous++)
                {
                    var previousLine = lines[lineStart + previous];
                    JobLine projectedLine;
                    var determinant = MathUtil.Det(line.direction, previousLine.direction);

                    if (math.abs(determinant) <= MathUtil.RvoEpsilon)
                    {
                        if (math.dot(line.direction, previousLine.direction) > 0.0f)
                        {
                            continue;
                        }

                        projectedLine.point = 0.5f * (line.point + previousLine.point);
                    }
                    else
                    {
                        projectedLine.point = line.point +
                                              (MathUtil.Det(previousLine.direction, line.point - previousLine.point) /
                                               determinant) * line.direction;
                    }

                    projectedLine.direction = MathUtil.NormalizeSafe(previousLine.direction - line.direction);
                    tempLines[tempStart + projectionLineCount] = projectedLine;
                    projectionLineCount++;
                }

                var tempResult = result;
                var direction = new float3(-line.direction.y, line.direction.x, 0.0f);
                if (LinearProgram2(tempLines, tempStart, projectionLineCount, radius, direction, true, ref result) <
                    projectionLineCount)
                {
                    result = tempResult;
                }

                distance = MathUtil.Det(line.direction, line.point - result);
            }
        }
    }
}