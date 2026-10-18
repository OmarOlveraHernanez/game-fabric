using System;

namespace RescatePapi.Logic
{
    /// <summary>
    /// Movimiento vertical del jugador: salto de altura variable (mantener = mas alto),
    /// caida mas rapida y buffer de salto. Sin dependencias de Unity.
    /// </summary>
    public sealed class JumpMotor
    {
        readonly RunnerConfig _c;
        float _buffer;
        float _jumpTimer;
        bool _jumpHeld;

        public float Y { get; private set; }
        public float VelocityY { get; private set; }
        public bool IsGrounded => Y <= 0f && VelocityY <= 0f;

        /// <summary>Se dispara en el frame en que inicia un salto.</summary>
        public event Action Jumped;

        public JumpMotor(RunnerConfig config) { _c = config; }

        public void Reset()
        {
            Y = 0f; VelocityY = 0f; _buffer = 0f; _jumpTimer = 0f; _jumpHeld = false;
        }

        public void Tick(float dt, bool pressedThisFrame, bool held)
        {
            _buffer = pressedThisFrame ? _c.JumpBufferTime : Math.Max(0f, _buffer - dt);

            if (_buffer > 0f && IsGrounded)
            {
                VelocityY = _c.JumpVelocity;
                _buffer = 0f;
                _jumpTimer = 0f;
                _jumpHeld = true;
                Jumped?.Invoke();
            }

            if (_jumpHeld)
            {
                _jumpTimer += dt;
                if (!held && _jumpTimer >= _c.MinJumpTime)
                {
                    if (VelocityY > 0f) VelocityY *= _c.JumpCutMultiplier;
                    _jumpHeld = false;
                }
            }

            float g = VelocityY > 0f ? _c.Gravity : _c.Gravity * _c.FallGravityMultiplier;
            VelocityY -= g * dt;
            Y += VelocityY * dt;

            if (Y <= 0f)
            {
                Y = 0f;
                VelocityY = 0f;
                _jumpHeld = false;
            }
        }
    }
}
