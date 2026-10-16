using Godot;

public partial class Main : Node2D
{
	private const float Width = 960f;
	private const float Height = 540f;
	private const float PaddleWidth = 18f;
	private const float PaddleHeight = 110f;
	private const float PaddleSpeed = 420f;
	private const float OpponentSpeed = 330f;
	private const float BallRadius = 10f;

	private float _leftY;
	private float _rightY;
	private Vector2 _ball;
	private Vector2 _velocity;
	private int _leftScore;
	private int _rightScore;
	private bool _gameOver;
	private readonly RandomNumberGenerator _random = new();

	public override void _Ready()
	{
		_random.Randomize();
		ResetGame();
		QueueRedraw();
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;

		if (Input.IsActionJustPressed("restart"))
			ResetGame();

		if (!_gameOver)
		{
			MovePaddles(dt);
			MoveBall(dt);
		}

		QueueRedraw();
	}

	private void MovePaddles(float dt)
	{
		float leftDirection = Input.GetAxis("left_up", "left_down");
		_leftY = Mathf.Clamp(_leftY + leftDirection * PaddleSpeed * dt, PaddleHeight / 2f, Height - PaddleHeight / 2f);

		// The red paddle follows the ball, but its movement is deliberately limited
		// so the player can still outplay it with well-placed shots.
		float difference = _ball.Y - _rightY;
		if (Mathf.Abs(difference) > 8f)
			_rightY += Mathf.Sign(difference) * Mathf.Min(Mathf.Abs(difference), OpponentSpeed * dt);
		_rightY = Mathf.Clamp(_rightY, PaddleHeight / 2f, Height - PaddleHeight / 2f);
	}

	private void MoveBall(float dt)
	{
		_ball += _velocity * dt;

		if (_ball.Y - BallRadius <= 0f || _ball.Y + BallRadius >= Height)
		{
			_ball.Y = Mathf.Clamp(_ball.Y, BallRadius, Height - BallRadius);
			_velocity.Y *= -1f;
		}

		Rect2 leftPaddle = new(32f, _leftY - PaddleHeight / 2f, PaddleWidth, PaddleHeight);
		Rect2 rightPaddle = new(Width - 32f - PaddleWidth, _rightY - PaddleHeight / 2f, PaddleWidth, PaddleHeight);
		Rect2 ballRect = new(_ball - Vector2.One * BallRadius, Vector2.One * BallRadius * 2f);

		if (_velocity.X < 0 && ballRect.Intersects(leftPaddle))
			BounceFromPaddle(leftPaddle, 1f);
		else if (_velocity.X > 0 && ballRect.Intersects(rightPaddle))
			BounceFromPaddle(rightPaddle, -1f);

		if (_ball.X < -BallRadius)
		{
			_rightScore++;
			AfterPoint();
		}
		else if (_ball.X > Width + BallRadius)
		{
			_leftScore++;
			AfterPoint();
		}
	}

	private void BounceFromPaddle(Rect2 paddle, float direction)
	{
		float relativeHit = (_ball.Y - paddle.Position.Y - paddle.Size.Y / 2f) / (paddle.Size.Y / 2f);
		_ball.X = direction > 0 ? paddle.End.X + BallRadius : paddle.Position.X - BallRadius;
		_velocity = new Vector2(direction * Mathf.Abs(_velocity.X) * 1.04f, relativeHit * 320f);
		if (_velocity.Length() > 760f)
			_velocity = _velocity.Normalized() * 760f;
	}

	private void AfterPoint()
	{
		if (_leftScore >= 10 || _rightScore >= 10)
		{
			_gameOver = true;
			_ball = new Vector2(Width / 2f, Height / 2f);
			return;
		}

		ResetBall(_leftScore > _rightScore ? -1f : 1f);
	}

	private void ResetGame()
	{
		_leftScore = 0;
		_rightScore = 0;
		_gameOver = false;
		_leftY = Height / 2f;
		_rightY = Height / 2f;
		ResetBall(_random.Randf() > 0.5f ? 1f : -1f);
	}

	private void ResetBall(float direction)
	{
		_ball = new Vector2(Width / 2f, Height / 2f);
		float angle = _random.RandfRange(-0.55f, 0.55f);
		_velocity = new Vector2(direction * 430f, angle * 430f);
	}

	public override void _Draw()
	{
		DrawRect(new Rect2(0, 0, Width, Height), new Color("090b16"));
		for (int y = 18; y < Height; y += 34)
			DrawRect(new Rect2(Width / 2f - 2f, y, 4f, 18f), new Color("32364f"));

		DrawRect(new Rect2(32, _leftY - PaddleHeight / 2f, PaddleWidth, PaddleHeight), new Color("68d9ff"));
		DrawRect(new Rect2(Width - 32 - PaddleWidth, _rightY - PaddleHeight / 2f, PaddleWidth, PaddleHeight), new Color("ff6e91"));
		DrawCircle(_ball, BallRadius, Colors.White);

		Font font = ThemeDB.FallbackFont;
		DrawString(font, new Vector2(Width / 2f - 100f, 76f), $"{_leftScore}     {_rightScore}", HorizontalAlignment.Left, -1, 46, Colors.White);
		DrawString(font, new Vector2(28f, Height - 20f), "W / S", HorizontalAlignment.Left, -1, 18, new Color("68d9ff"));
		DrawString(font, new Vector2(Width - 180f, Height - 20f), "RED: COMPUTER", HorizontalAlignment.Left, -1, 18, new Color("ff6e91"));

		if (_gameOver)
		{
			string winner = _leftScore > _rightScore ? "LEFT PLAYER WINS" : "RIGHT PLAYER WINS";
			DrawString(font, new Vector2(Width / 2f - 190f, Height / 2f - 10f), winner, HorizontalAlignment.Left, -1, 30, Colors.White);
			DrawString(font, new Vector2(Width / 2f - 120f, Height / 2f + 30f), "Press R to restart", HorizontalAlignment.Left, -1, 20, new Color("aeb4d0"));
		}
	}
}
