using System;
using Godot;

public partial class Player : CharacterBody3D
{
    [Export] public float Speed = 6f;
    [Export] public float JumpVelocity = 5f;
    [Export] public float MouseSensitivity = 0.002f;
    [Export] public int Health = 100;

    public bool AirControl = true;

    public float DashForce = 13f;
    public float DashDuration = 0.3f;
    public float DashCooldown = 1f;

    public float currentFov;
    public float targetFov;
    public float baseFov;
    public float DashFovBonus = 8f;
    public float FovSpeed = 12.5f;

    public float Acceleration = 15f;
    public float Deceleration = 17f;

    public float WalkTiltAmount = 0.04f;
    public float DashTiltAmount = 0.5f;
    public float TiltSpeed = 8f;
    public float TiltReturnSpeed = 8f;
    public float targetTilt = 0f;
    public float currentTilt = 0f;
    public float cameraRotationX = 0f;

    public bool isDashing = false;
    public float dashTimer = 0f;
    public float dashCooldownTimer = 0f;
    private Vector3 dashDirection = Vector3.Zero;

    private Node3D head;
    private Camera3D camera;

    private float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

    private enum PlayerState
    {
        Normal,
        Dash
    }

    private PlayerState currentState = PlayerState.Normal;

    private Vector2 input;

    public override void _Ready()
    {
        head = GetNode<Node3D>("Head");
        camera = head.GetNode<Camera3D>("Camera3D");
        baseFov = camera.Fov;
        currentFov = baseFov;
        targetFov = baseFov;
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is InputEventMouseMotion mouse)
        {
            RotateY(-mouse.Relative.X * MouseSensitivity);

            cameraRotationX -= mouse.Relative.Y * MouseSensitivity;
            cameraRotationX = Mathf.Clamp(cameraRotationX, -Mathf.Pi / 2, Mathf.Pi / 2);

            head.Rotation = new Vector3(cameraRotationX, 0, 0);
        }

        if (Input.IsActionJustPressed("ui_cancel"))
        {
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        float d = (float)delta;

        HandleGravity(d);
        HandleJump();
        HandleDashInput();

        input = Input.GetVector(
            "move_left",
            "move_right",
            "move_forward",
            "move_back"
        );

        if (dashCooldownTimer > 0)
            dashCooldownTimer -= d;

        if (currentState == PlayerState.Normal)
        {
            float localX = (Transform.Basis.X).Dot(
                (Transform.Basis.X * input.X + Transform.Basis.Z * input.Y).Normalized()
            );
            targetTilt = -localX * WalkTiltAmount;
        }

        if (Input.IsActionJustPressed("damage"))
        {
            Health -= 10;
            GD.Print($"Health: {Health}");

            if (Health < 0)
            {
                System.Environment.FailFast("Health < 0");
            }
        }

        switch (currentState)
        {
            case PlayerState.Dash:
                HandleDash(d);
                break;

            case PlayerState.Normal:
                HandleMovement(d, input);
                break;
        }

        currentTilt = Mathf.Lerp(currentTilt, targetTilt, TiltSpeed * d);
        targetTilt = Mathf.Lerp(targetTilt, 0f, TiltReturnSpeed * d);

        currentFov = Mathf.Lerp(currentFov, targetFov, FovSpeed * d);
        camera.Fov = currentFov;

        head.Rotation = new Vector3(cameraRotationX, 0, currentTilt);

        MoveAndSlide();


    }

    private void HandleDash(float d)
    {
        dashTimer -= d;

        Velocity = new Vector3(
            dashDirection.X * DashForce,
            Velocity.Y,
            dashDirection.Z * DashForce
        );

        if (dashTimer <= 0f)
        {
            currentState = PlayerState.Normal;
            targetFov = baseFov;
        }
    }

    private void HandleGravity(float d)
    {
        if (!IsOnFloor())
            Velocity = new Vector3(Velocity.X, Velocity.Y - gravity * d, Velocity.Z);
    }

    private void HandleJump()
    {
        if (Input.IsActionJustPressed("jump") && IsOnFloor())
            Velocity = new Vector3(Velocity.X, JumpVelocity, Velocity.Z);
    }

    private void HandleDashInput()
    {
        if (Input.IsActionJustPressed("dash") && dashCooldownTimer <= 0 && input != Vector2.Zero)
        {
            dashDirection =
                (Transform.Basis.X * input.X +
                 Transform.Basis.Z * input.Y).Normalized();

            float localX = dashDirection.Dot(Transform.Basis.X);
            targetTilt = -localX * DashTiltAmount;

            float forwardDot = dashDirection.Dot(-Transform.Basis.Z);
            if (Mathf.Abs(forwardDot) > 0.45f || Mathf.Abs(forwardDot) < -0.45f)
                targetFov = baseFov + DashFovBonus;

            dashTimer = DashDuration;
            dashCooldownTimer = DashCooldown;
            currentState = PlayerState.Dash;
        }
    }

    private void HandleMovement(float d, Vector2 input)
    {
        Vector3 direction =
            Transform.Basis.X * input.X +
            Transform.Basis.Z * input.Y;

        direction = direction.Normalized();

        Vector3 horizontalVelocity = new Vector3(Velocity.X, 0, Velocity.Z);

        if (!IsOnFloor() && !AirControl)
            return;

        if (direction != Vector3.Zero)
        {
            horizontalVelocity = horizontalVelocity.Lerp(
                direction * Speed,
                Acceleration * d
            );
        }
        else
        {
            horizontalVelocity = horizontalVelocity.Lerp(
                Vector3.Zero,
                Deceleration * d
            );
        }

        Velocity = new Vector3(
            horizontalVelocity.X,
            Velocity.Y,
            horizontalVelocity.Z
        );
    }
}