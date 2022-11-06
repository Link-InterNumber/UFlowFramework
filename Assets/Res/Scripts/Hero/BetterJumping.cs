using Res.Scripts.Hero;
using UnityEngine;

public class BetterJumping : MonoBehaviour
{
    private Rigidbody2D _rb;
    public float fallMultiplier = 2.5f;
    public float lowJumpMultiplier = 2f;

    void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    void LateUpdate()
    {
        if(_rb.velocity.y < 0)
        {
            _rb.velocity += Vector2.up * (Physics2D.gravity.y * (fallMultiplier - 1) * Time.deltaTime);
        }else if(_rb.velocity.y > 0 && !PlayerCtrlInput.Instance.GetInput().IsPressJump)
        {
            _rb.velocity += Vector2.up * (Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.deltaTime);
        }
    }
}