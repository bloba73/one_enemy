using Godot;

public partial class Player : CharacterBody3D
{
    [Export] public float Speed = 4f;
    [Export] public float SprintSpeed = 6f;
    [Export] public float JumpVelocity = 4.5f;
    [Export] public float MouseSensitivity = 0.002f;

    [Export] public float DashForce = 13f;
    [Export] public float DashDuration = 0.2f;
    [Export] public float DashCooldown = 1f;

    public float Acceleration = 10f;
    public float Deceleration = 12f;

    public float TiltAmount = 0.04f;
    public float TiltSpeed = 10f;
    public float TiltReturnSpeed = 6f;
    private float targetTilt = 0f;

    private bool isDashing = false;
    private float dashTimer = 0f;
    private float dashCooldownTimer = 0f;
    private Vector3 dashDirection = Vector3.Zero;

    private Node3D head;

    private float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
    private float cameraRotationX = 0f;
    private float currentTilt = 0f;

    public override void _Ready()
    {
        head = GetNode<Node3D>("Head");
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

        if (!IsOnFloor())
            Velocity = new Vector3(Velocity.X, Velocity.Y - gravity * d, Velocity.Z);

        if (Input.IsActionJustPressed("jump") && IsOnFloor())
            Velocity = new Vector3(Velocity.X, JumpVelocity, Velocity.Z);

        Vector2 input = Input.GetVector(
            "move_left",
            "move_right",
            "move_forward",
            "move_back"
        );

        if (dashCooldownTimer > 0)
            dashCooldownTimer -= d;

        if (Input.IsActionJustPressed("dash") && dashCooldownTimer <= 0)
        {
            if (input != Vector2.Zero)
            {
                dashDirection =
                    (Transform.Basis.X * input.X +
                    Transform.Basis.Z * input.Y).Normalized();

                float localX = dashDirection.Dot(Transform.Basis.X);
                targetTilt = -localX * TiltAmount;

                isDashing = true;
                dashTimer = DashDuration;
                dashCooldownTimer = DashCooldown;
            }
        }

        if (isDashing)
        {
            dashTimer -= d;

            Velocity = new Vector3(
                dashDirection.X * DashForce,
                0,
                dashDirection.Z * DashForce
            );

            if (dashTimer <= 0)
            {
                isDashing = false;
            }

            MoveAndSlide();
            return;
        }

        float currentSpeed = Input.IsActionPressed("sprint") ? SprintSpeed : Speed;

        Vector3 direction =
            Transform.Basis.X * input.X +
            Transform.Basis.Z * input.Y;

        direction = direction.Normalized();

        Vector3 horizontalVelocity = new Vector3(Velocity.X, 0, Velocity.Z);

        if (direction != Vector3.Zero)
        {
            horizontalVelocity = horizontalVelocity.Lerp(
                direction * currentSpeed,
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

        currentTilt = Mathf.Lerp(currentTilt, targetTilt, TiltSpeed * d);
        targetTilt = Mathf.Lerp(targetTilt, 0f, TiltReturnSpeed * d);

        head.Rotation = new Vector3(cameraRotationX, 0, currentTilt);

        MoveAndSlide();
    }
}