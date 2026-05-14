using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PowerCellStudio
{
    public class BinarySerializerTest : RunTestMono
    {
        private void OnEnable()
        {
            Debug.Log("========== BinarySerializer Test Suite Started ==========");

            BinarySerializer.RegisterCustomSelector(new CustomEncodedValueSelector());

            TestPrimitiveRoundTrip();
            TestCommonValueTypeRoundTrip();
            TestArrayRoundTrip();
            TestConcreteCollectionRoundTrip();
            TestUnityVector3RoundTrip();
            TestNestedObjectRoundTrip();
            TestInterfaceCollectionFieldRoundTrip();
            TestQueueAndStackRoundTrip();
            TestKeyValuePairRoundTrip();
            TestNestedCustomSelectorRoundTrip();
            TestTypeWithoutParameterlessConstructorRoundTrip();
            TestSerializeFieldPrivateFieldRoundTrip();
            TestSerializableCtorlessSerializeFieldPrivateFieldRoundTrip();
            TestNullFieldRoundTrip();
            TestNullRootRoundTrip();
            TestSpanRoundTrip();
            TestSpanAndByteArrayCompatibility();

            Debug.Log("========== BinarySerializer Test Suite Finished ==========");
        }

        private void TestPrimitiveRoundTrip()
        {
            RunTest("Primitive RoundTrip", () =>
            {
                PrimitiveContainer source = new PrimitiveContainer
                {
                    BoolValue = true,
                    IntValue = 123456,
                    FloatValue = 12.5f,
                    DoubleValue = 88.125,
                    DecimalValue = 9.75m,
                    CharValue = 'K',
                    StringValue = "serialization-ok",
                    EnumValue = TestEnum.Beta
                };

                PrimitiveContainer clone = BinarySerializer.Deserialize<PrimitiveContainer>(BinarySerializer.Serialize(source));

                Assert(clone.BoolValue == source.BoolValue, "Bool roundtrip failed.");
                Assert(clone.IntValue == source.IntValue, "Int roundtrip failed.");
                Assert(Math.Abs(clone.FloatValue - source.FloatValue) < 0.0001f, "Float roundtrip failed.");
                Assert(Math.Abs(clone.DoubleValue - source.DoubleValue) < 0.0001d, "Double roundtrip failed.");
                Assert(clone.DecimalValue == source.DecimalValue, "Decimal roundtrip failed.");
                Assert(clone.CharValue == source.CharValue, "Char roundtrip failed.");
                Assert(clone.StringValue == source.StringValue, "String roundtrip failed.");
                Assert(clone.EnumValue == source.EnumValue, "Enum roundtrip failed.");
            });
        }

        private void TestCommonValueTypeRoundTrip()
        {
            RunTest("Common ValueType RoundTrip", () =>
            {
                ValueTypeContainer source = new ValueTypeContainer
                {
                    GuidValue = Guid.NewGuid(),
                    TimeSpanValue = TimeSpan.FromMinutes(135),
                    DateTimeValue = new DateTime(2024, 5, 6, 7, 8, 9, DateTimeKind.Utc),
                    DateTimeOffsetValue = new DateTimeOffset(2024, 5, 6, 7, 8, 9, TimeSpan.FromHours(8))
                };

                ValueTypeContainer clone = BinarySerializer.Deserialize<ValueTypeContainer>(BinarySerializer.Serialize(source));

                Assert(clone.GuidValue == source.GuidValue, "Guid roundtrip failed.");
                Assert(clone.TimeSpanValue == source.TimeSpanValue, "TimeSpan roundtrip failed.");
                Assert(clone.DateTimeValue == source.DateTimeValue, "DateTime roundtrip failed.");
                Assert(clone.DateTimeOffsetValue.Equals(source.DateTimeOffsetValue), "DateTimeOffset roundtrip failed.");
            });
        }

        private void TestNestedObjectRoundTrip()
        {
            RunTest("Nested Object RoundTrip", () =>
            {
                ComplexContainer source = new ComplexContainer
                {
                    Name = "nested-root",
                    Child = new NestedNode
                    {
                        Label = "child-node",
                        Weight = 42
                    }
                };

                ComplexContainer clone = BinarySerializer.Deserialize<ComplexContainer>(BinarySerializer.Serialize(source));

                Assert(clone != null, "Nested object clone should not be null.");
                Assert(clone.Child != null, "Nested child should not be null.");
                Assert(clone.Name == source.Name, "Nested parent field mismatch.");
                Assert(clone.Child.Label == source.Child.Label, "Nested child label mismatch.");
                Assert(clone.Child.Weight == source.Child.Weight, "Nested child weight mismatch.");
            });
        }

        private void TestArrayRoundTrip()
        {
            RunTest("Array RoundTrip", () =>
            {
                ArrayContainer source = new ArrayContainer
                {
                    Numbers = new[] { 4, 8, 15, 16, 23, 42 },
                    Labels = new[] { "alpha", null, "gamma" }
                };

                ArrayContainer clone = BinarySerializer.Deserialize<ArrayContainer>(BinarySerializer.Serialize(source));

                Assert(clone != null, "Array container should deserialize.");
                Assert(clone.Numbers != null, "Int array should deserialize.");
                Assert(clone.Labels != null, "String array should deserialize.");
                Assert(clone.Numbers.SequenceEqual(source.Numbers), "Int array values mismatch.");
                Assert(clone.Labels.Length == source.Labels.Length, "String array length mismatch.");
                Assert(clone.Labels[0] == source.Labels[0], "String array first value mismatch.");
                Assert(clone.Labels[1] == null, "String array null element mismatch.");
                Assert(clone.Labels[2] == source.Labels[2], "String array last value mismatch.");
            });
        }

        private void TestConcreteCollectionRoundTrip()
        {
            RunTest("Concrete Collection RoundTrip", () =>
            {
                List<int> sourceList = new List<int> { 2, 4, 6, 8 };
                Dictionary<string, int> sourceDictionary = new Dictionary<string, int>
                {
                    { "coins", 77 },
                    { "gems", 12 }
                };
                HashSet<string> sourceSet = new HashSet<string> { "red", "blue" };

                List<int> cloneList = BinarySerializer.Deserialize<List<int>>(BinarySerializer.Serialize(sourceList));
                Dictionary<string, int> cloneDictionary = BinarySerializer.Deserialize<Dictionary<string, int>>(BinarySerializer.Serialize(sourceDictionary));
                HashSet<string> cloneSet = BinarySerializer.Deserialize<HashSet<string>>(BinarySerializer.Serialize(sourceSet));

                Assert(cloneList != null, "Concrete List should deserialize.");
                Assert(cloneDictionary != null, "Concrete Dictionary should deserialize.");
                Assert(cloneSet != null, "Concrete HashSet should deserialize.");
                Assert(cloneList.SequenceEqual(sourceList), "Concrete List values mismatch.");
                Assert(cloneDictionary.Count == sourceDictionary.Count, "Concrete Dictionary count mismatch.");
                Assert(cloneDictionary["coins"] == 77 && cloneDictionary["gems"] == 12, "Concrete Dictionary values mismatch.");
                Assert(cloneSet.SetEquals(sourceSet), "Concrete HashSet values mismatch.");
            });
        }

        private void TestUnityVector3RoundTrip()
        {
            RunTest("Unity Vector3 RoundTrip", () =>
            {
                Vector3Container source = new Vector3Container
                {
                    Position = new Vector3(1.5f, -2.25f, 99.75f),
                    Nested = new Vector3NestedContainer
                    {
                        Offset = new Vector3(-7f, 8.5f, 0.125f)
                    }
                };

                Vector3Container clone = BinarySerializer.Deserialize<Vector3Container>(BinarySerializer.Serialize(source));

                Assert(clone != null, "Vector3 container should deserialize.");
                Assert(clone.Position == source.Position, "Vector3 field mismatch.");
                Assert(clone.Nested != null, "Nested Vector3 container should deserialize.");
                Assert(clone.Nested.Offset == source.Nested.Offset, "Nested Vector3 field mismatch.");
            });
        }

        private void TestInterfaceCollectionFieldRoundTrip()
        {
            RunTest("Interface Collection Field RoundTrip", () =>
            {
                InterfaceCollectionContainer source = new InterfaceCollectionContainer
                {
                    Items = new List<int> { 3, 5, 8 },
                    Mappings = new Dictionary<string, int>
                    {
                        { "hp", 100 },
                        { "mp", 60 }
                    },
                    Tags = new HashSet<string> { "alpha", "beta" }
                };
                InterfaceCollectionContainer clone = BinarySerializer.Deserialize<InterfaceCollectionContainer>(BinarySerializer.Serialize(source));

                Assert(clone.Items != null, "IList field should be restored.");
                Assert(clone.Mappings != null, "IDictionary field should be restored.");
                Assert(clone.Tags != null, "ISet field should be restored.");
                Assert(clone.Items is List<int>, "IList field should deserialize to List<int>.");
                Assert(clone.Mappings is Dictionary<string, int>, "IDictionary field should deserialize to Dictionary<string, int>.");
                Assert(clone.Tags is HashSet<string>, "ISet field should deserialize to HashSet<string>.");
                Assert(clone.Items.SequenceEqual(source.Items), "IList field values mismatch.");
                Assert(clone.Mappings.Count == source.Mappings.Count, "IDictionary field count mismatch.");
                Assert(clone.Mappings["hp"] == 100 && clone.Mappings["mp"] == 60, "IDictionary field values mismatch.");
                Assert(clone.Tags.SetEquals(source.Tags), "ISet field values mismatch.");
            });
        }

        private void TestQueueAndStackRoundTrip()
        {
            RunTest("Queue And Stack RoundTrip", () =>
            {
                QueueStackContainer source = new QueueStackContainer
                {
                    Queue = new Queue<int>(new[] { 1, 2, 3 }),
                    Stack = new Stack<int>(new[] { 1, 2, 3 })
                };

                QueueStackContainer clone = BinarySerializer.Deserialize<QueueStackContainer>(BinarySerializer.Serialize(source));

                Assert(clone.Queue.SequenceEqual(source.Queue), "Queue order mismatch.");
                Assert(clone.Stack.SequenceEqual(source.Stack), "Stack order mismatch.");
                Assert(clone.Stack.Peek() == source.Stack.Peek(), "Stack top mismatch.");
            });
        }

        private void TestKeyValuePairRoundTrip()
        {
            RunTest("KeyValuePair RoundTrip", () =>
            {
                KeyValuePair<string, int> source = new KeyValuePair<string, int>("coins", 77);
                KeyValuePair<string, int> clone = BinarySerializer.Deserialize<KeyValuePair<string, int>>(BinarySerializer.Serialize(source));

                Assert(clone.Key == source.Key, "KeyValuePair key mismatch.");
                Assert(clone.Value == source.Value, "KeyValuePair value mismatch.");
            });
        }

        private void TestNestedCustomSelectorRoundTrip()
        {
            RunTest("Nested Custom Selector RoundTrip", () =>
            {
                CustomSelectorContainer source = new CustomSelectorContainer
                {
                    Payload = new CustomEncodedValue
                    {
                        Number = 7,
                        Text = "nested-selector"
                    },
                    Payloads = new List<CustomEncodedValue>
                    {
                        new CustomEncodedValue { Number = 1, Text = "one" },
                        new CustomEncodedValue { Number = 2, Text = "two" }
                    }
                };

                CustomSelectorContainer clone = BinarySerializer.Deserialize<CustomSelectorContainer>(BinarySerializer.Serialize(source));

                Assert(clone.Payload != null, "Custom selector field should be restored.");
                Assert(clone.Payload.Number == source.Payload.Number, "Custom selector field number mismatch.");
                Assert(clone.Payload.Text == source.Payload.Text, "Custom selector field text mismatch.");
                Assert(clone.Payloads != null && clone.Payloads.Count == 2, "Custom selector list should be restored.");
                Assert(clone.Payloads[0].Text == "one" && clone.Payloads[1].Text == "two", "Custom selector list values mismatch.");
            });
        }

        private void TestNullFieldRoundTrip()
        {
            RunTest("Null Field RoundTrip", () =>
            {
                NullableFieldContainer source = new NullableFieldContainer
                {
                    Name = null,
                    Child = null
                };

                NullableFieldContainer clone = BinarySerializer.Deserialize<NullableFieldContainer>(BinarySerializer.Serialize(source));

                Assert(clone.Name == null, "Null string field should stay null.");
                Assert(clone.Child == null, "Null object field should stay null.");
            });
        }

        private void TestNullRootRoundTrip()
        {
            RunTest("Null Root RoundTrip", () =>
            {
                string cloneString = BinarySerializer.Deserialize<string>(BinarySerializer.Serialize<string>(null));
                CustomEncodedValue cloneCustom = BinarySerializer.Deserialize<CustomEncodedValue>(BinarySerializer.Serialize<CustomEncodedValue>(null));

                Assert(cloneString == null, "Null root string should stay null.");
                Assert(cloneCustom == null, "Null root custom selector value should stay null.");
            });
        }

        private void TestSpanRoundTrip()
        {
            RunTest("Span RoundTrip", () =>
            {
                PrimitiveContainer source = new PrimitiveContainer
                {
                    BoolValue = true,
                    IntValue = 314159,
                    FloatValue = 3.5f,
                    DoubleValue = 6.25d,
                    DecimalValue = 12.75m,
                    CharValue = 'S',
                    StringValue = "span-roundtrip",
                    EnumValue = TestEnum.Gamma
                };
                ArrayBufferWriter<byte> bufferWriter = new ArrayBufferWriter<byte>();
                BinarySerializer.SerializeAsSpan(source, bufferWriter);
                PrimitiveContainer clone = BinarySerializer.Deserialize<PrimitiveContainer>(bufferWriter.WrittenSpan);

                Assert(bufferWriter.WrittenCount > 0, "SerializeAsSpan should produce bytes.");
                Assert(clone != null, "Span roundtrip clone should not be null.");
                Assert(clone.BoolValue == source.BoolValue, "Span bool roundtrip failed.");
                Assert(clone.IntValue == source.IntValue, "Span int roundtrip failed.");
                Assert(Math.Abs(clone.FloatValue - source.FloatValue) < 0.0001f, "Span float roundtrip failed.");
                Assert(Math.Abs(clone.DoubleValue - source.DoubleValue) < 0.0001d, "Span double roundtrip failed.");
                Assert(clone.DecimalValue == source.DecimalValue, "Span decimal roundtrip failed.");
                Assert(clone.CharValue == source.CharValue, "Span char roundtrip failed.");
                Assert(clone.StringValue == source.StringValue, "Span string roundtrip failed.");
                Assert(clone.EnumValue == source.EnumValue, "Span enum roundtrip failed.");
            });
        }

        private void TestSpanAndByteArrayCompatibility()
        {
            RunTest("Span And ByteArray Compatibility", () =>
            {
                CustomSelectorContainer source = new CustomSelectorContainer
                {
                    Payload = new CustomEncodedValue
                    {
                        Number = 9,
                        Text = "span-compatibility"
                    },
                    Payloads = new List<CustomEncodedValue>
                    {
                        new CustomEncodedValue { Number = 10, Text = "ten" },
                        new CustomEncodedValue { Number = 11, Text = "eleven" }
                    }
                };

                byte[] byteArrayData = BinarySerializer.Serialize(source);
                ArrayBufferWriter<byte> bufferWriter = new ArrayBufferWriter<byte>();
                BinarySerializer.SerializeAsSpan(source, bufferWriter);

                CustomSelectorContainer cloneFromByteArrayAsSpan = BinarySerializer.Deserialize<CustomSelectorContainer>(byteArrayData.AsSpan());
                CustomSelectorContainer cloneFromSpan = BinarySerializer.Deserialize<CustomSelectorContainer>(bufferWriter.WrittenSpan);

                Assert(bufferWriter.WrittenCount > 0, "Span compatibility data should not be empty.");
                Assert(byteArrayData.Length > 0, "Byte array compatibility data should not be empty.");
                Assert(cloneFromByteArrayAsSpan != null, "Deserialize from byte[] AsSpan should succeed.");
                Assert(cloneFromSpan != null, "Deserialize from SerializeAsSpan should succeed.");
                Assert(cloneFromByteArrayAsSpan.Payload != null, "Byte[] AsSpan payload should deserialize.");
                Assert(cloneFromSpan.Payload != null, "Span payload should deserialize.");
                Assert(cloneFromByteArrayAsSpan.Payload.Number == source.Payload.Number, "Byte[] AsSpan payload number mismatch.");
                Assert(cloneFromByteArrayAsSpan.Payload.Text == source.Payload.Text, "Byte[] AsSpan payload text mismatch.");
                Assert(cloneFromSpan.Payload.Number == source.Payload.Number, "Span payload number mismatch.");
                Assert(cloneFromSpan.Payload.Text == source.Payload.Text, "Span payload text mismatch.");
                Assert(cloneFromByteArrayAsSpan.Payloads.Count == source.Payloads.Count, "Byte[] AsSpan payload list count mismatch.");
                Assert(cloneFromSpan.Payloads.Count == source.Payloads.Count, "Span payload list count mismatch.");
                Assert(cloneFromByteArrayAsSpan.Payloads[0].Text == source.Payloads[0].Text, "Byte[] AsSpan payload list first item mismatch.");
                Assert(cloneFromSpan.Payloads[1].Text == source.Payloads[1].Text, "Span payload list second item mismatch.");
            });
        }

        private void TestTypeWithoutParameterlessConstructorRoundTrip()
        {
            RunTest("No Parameterless Constructor RoundTrip", () =>
            {
                ConstructorOnlyContainer source = new ConstructorOnlyContainer("constructor-only", 9)
                {
                    Description = "restored-from-fields"
                };

                ConstructorOnlyContainer clone = BinarySerializer.Deserialize<ConstructorOnlyContainer>(BinarySerializer.Serialize(source));

                Assert(clone != null, "Type without parameterless constructor should deserialize.");
                Assert(clone.Name == source.Name, "Constructor-only type name mismatch.");
                Assert(clone.Level == source.Level, "Constructor-only type level mismatch.");
                Assert(clone.Description == source.Description, "Constructor-only type description mismatch.");
            });
        }

        private void TestSerializeFieldPrivateFieldRoundTrip()
        {
            RunTest("SerializeField Private Field RoundTrip", () =>
            {
                SerializeFieldPrivateContainer source = new SerializeFieldPrivateContainer();
                source.SetValues(27, "hidden-text");

                SerializeFieldPrivateContainer clone = BinarySerializer.Deserialize<SerializeFieldPrivateContainer>(BinarySerializer.Serialize(source));

                Assert(clone != null, "SerializeField private field container should deserialize.");
                Assert(clone.GetHiddenNumber() == 27, "[SerializeField] private int field mismatch.");
                Assert(clone.GetHiddenText() == "hidden-text", "[SerializeField] private string field mismatch.");
                Assert(clone.PublicMirror == source.PublicMirror, "Public field should still roundtrip.");
            });
        }

        private void TestSerializableCtorlessSerializeFieldPrivateFieldRoundTrip()
        {
            RunTest("Serializable Ctorless SerializeField Private Field RoundTrip", () =>
            {
                SerializableCtorlessPrivateFieldContainer source = new SerializableCtorlessPrivateFieldContainer("ctorless-private", 31);
                source.SetHiddenValues(64, "hidden-serialized");

                SerializableCtorlessPrivateFieldContainer clone = BinarySerializer.Deserialize<SerializableCtorlessPrivateFieldContainer>(BinarySerializer.Serialize(source));

                Assert(clone != null, "Serializable ctorless private field container should deserialize.");
                Assert(clone.Name == source.Name, "Ctorless private field container name mismatch.");
                Assert(clone.Level == source.Level, "Ctorless private field container level mismatch.");
                Assert(clone.GetHiddenNumber() == 64, "Ctorless [SerializeField] private int field mismatch.");
                Assert(clone.GetHiddenText() == "hidden-serialized", "Ctorless [SerializeField] private string field mismatch.");
                Assert(clone.PublicMirror == source.PublicMirror, "Ctorless public field should still roundtrip.");
            });
        }

        [Serializable]
        private class PrimitiveContainer
        {
            public bool BoolValue;
            public int IntValue;
            public float FloatValue;
            public double DoubleValue;
            public decimal DecimalValue;
            public char CharValue;
            public string StringValue;
            public TestEnum EnumValue;
        }

        [Serializable]
        private struct ValueTypeContainer
        {
            public Guid GuidValue;
            public TimeSpan TimeSpanValue;
            public DateTime DateTimeValue;
            public DateTimeOffset DateTimeOffsetValue;
        }

        [Serializable]
        private class ComplexContainer
        {
            public string Name;
            public NestedNode Child;
        }

        [Serializable]
        private class NestedNode
        {
            public string Label;
            public int Weight;
        }

        [Serializable]
        private class Vector3Container
        {
            public Vector3 Position;
            public Vector3NestedContainer Nested;
        }

        [Serializable]
        private class ArrayContainer
        {
            public int[] Numbers;
            public string[] Labels;
        }

        [Serializable]
        private class Vector3NestedContainer
        {
            public Vector3 Offset;
        }

        [Serializable]
        private class InterfaceCollectionContainer
        {
            public IList<int> Items;
            public IDictionary<string, int> Mappings;
            public ISet<string> Tags;
        }

        [Serializable]
        private class QueueStackContainer
        {
            public Queue<int> Queue;
            public Stack<int> Stack;
        }

        [Serializable]
        private class CustomSelectorContainer
        {
            public CustomEncodedValue Payload;
            public List<CustomEncodedValue> Payloads;
        }

        [Serializable]
        private class NullableFieldContainer
        {
            public string Name;
            public NestedNode Child;
        }

        [Serializable]
        private class ConstructorOnlyContainer
        {
            public string Name;
            public int Level;
            public string Description;

            public ConstructorOnlyContainer(string name, int level)
            {
                Name = name;
                Level = level;
            }
        }

        [Serializable]
        private class SerializeFieldPrivateContainer
        {
            [SerializeField]
            private int _hiddenNumber;

            [SerializeField]
            private string _hiddenText;

            public int PublicMirror;

            public void SetValues(int hiddenNumber, string hiddenText)
            {
                _hiddenNumber = hiddenNumber;
                _hiddenText = hiddenText;
                PublicMirror = hiddenNumber * 2;
            }

            public int GetHiddenNumber()
            {
                return _hiddenNumber;
            }

            public string GetHiddenText()
            {
                return _hiddenText;
            }
        }

        [Serializable]
        private class SerializableCtorlessPrivateFieldContainer
        {
            public string Name;
            public int Level;

            [SerializeField]
            private int _hiddenNumber;

            [SerializeField]
            private string _hiddenText;

            public int PublicMirror;

            public SerializableCtorlessPrivateFieldContainer(string name, int level)
            {
                Name = name;
                Level = level;
            }

            public void SetHiddenValues(int hiddenNumber, string hiddenText)
            {
                _hiddenNumber = hiddenNumber;
                _hiddenText = hiddenText;
                PublicMirror = hiddenNumber * 3;
            }

            public int GetHiddenNumber()
            {
                return _hiddenNumber;
            }

            public string GetHiddenText()
            {
                return _hiddenText;
            }
        }

        private class CustomEncodedValue
        {
            public int Number;
            public string Text;
        }

        private class CustomEncodedValueSelector : BinarySerializerTypeSelector<CustomEncodedValue>
        {
            public override void Write(BinaryWriter writer, CustomEncodedValue value, Encoding encoding)
            {
                writer.Write(value.Number);

                byte[] bytes = encoding.GetBytes(value.Text ?? string.Empty);
                writer.Write(bytes.Length);
                writer.Write(bytes);
            }

            public override CustomEncodedValue Read(BinaryReader reader, Encoding encoding)
            {
                int number = reader.ReadInt32();
                int length = reader.ReadInt32();
                string text = encoding.GetString(reader.ReadBytes(length));

                return new CustomEncodedValue
                {
                    Number = number,
                    Text = text
                };
            }
        }

        private enum TestEnum
        {
            Alpha,
            Beta,
            Gamma
        }
    }
}