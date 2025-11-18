using System;
using System.Collections.Generic;
using System.Drawing;

namespace Snake.Models
{
    public class GameState
    {
        public int BoardWidth { get; }
        public int BoardHeight { get; }
        public List<SnakeSegment> Snake { get; private set; }
        public Point FoodPosition { get; private set; }
        public int Score { get; private set; }
        public bool GameOver { get; private set; }
        public Direction CurrentDirection { get; private set; }

        private Random random;

        public enum Direction { Up, Down, Left, Right }

        public GameState(int width, int height)
        {
            BoardWidth = width;
            BoardHeight = height;
            random = new Random();
            Reset();
        }

        public void Reset()
        {
            Snake = new List<SnakeSegment>();
            Snake.Add(new SnakeSegment(BoardWidth / 2, BoardHeight / 2));
            Score = 0;
            GameOver = false;
            CurrentDirection = Direction.Right;
            GenerateFood();
        }

        public void GenerateFood()
        {
            int x, y;
            do
            {
                x = random.Next(0, BoardWidth);
                y = random.Next(0, BoardHeight);
            } while (IsOccupiedBySnake(x, y));

            FoodPosition = new Point(x, y);
        }

        private bool IsOccupiedBySnake(int x, int y)
        {
            foreach (var segment in Snake)
            {
                if (segment.X == x && segment.Y == y)
                    return true;
            }
            return false;
        }

        public void Move()
        {
            if (GameOver) return;

            var head = Snake[0];
            int newX = head.X;
            int newY = head.Y;

            switch (CurrentDirection)
            {
                case Direction.Up: newY--; break;
                case Direction.Down: newY++; break;
                case Direction.Left: newX--; break;
                case Direction.Right: newX++; break;
            }

            // Kollisionsprüfung mit Wänden
            if (newX < 0 || newX >= BoardWidth || newY < 0 || newY >= BoardHeight)
            {
                GameOver = true;
                return;
            }

            // WICHTIG: Kollisionsprüfung mit sich selbst!
            foreach (var segment in Snake)
            {
                if (segment.X == newX && segment.Y == newY)
                {
                    GameOver = true;
                    return;
                }
            }

            // Neue Position zur Schlange hinzufügen
            Snake.Insert(0, new SnakeSegment(newX, newY));

            // Essen gefressen?
            if (newX == FoodPosition.X && newY == FoodPosition.Y)
            {
                Score++;
                GenerateFood();
            }
            else
            {
                // Letztes Segment entfernen
                Snake.RemoveAt(Snake.Count - 1);
            }
        }

        public void ChangeDirection(Direction newDirection)
        {
            if (GameOver) return;

            // Keine 180-Grad-Wendung erlauben
            if ((CurrentDirection == Direction.Up && newDirection == Direction.Down) ||
                (CurrentDirection == Direction.Down && newDirection == Direction.Up) ||
                (CurrentDirection == Direction.Left && newDirection == Direction.Right) ||
                (CurrentDirection == Direction.Right && newDirection == Direction.Left))
            {
                return;
            }

            CurrentDirection = newDirection;
        }
    }
}
