using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.IO;
using System.Xml.Serialization;
using System.Speech.Synthesis;

namespace SnakeWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const int snakeSquareSize = 20;
        const int snakeStartLenght = 3;
        const int snakeStartSpeed = 400;
        const int snakeSpeedThreshold = 100;
        const int MaxHighScoreListEntryCount = 5;


        private Random random = new Random();
        private List<SnakePart> snakeParts = new List<SnakePart>();
        private SolidColorBrush headColor = Brushes.YellowGreen;
        private SolidColorBrush bodyColor = Brushes.Green;
        private UIElement snakeFood = null;
        private SolidColorBrush foodBrush = Brushes.Red;


        private enum SnakeDirection { Left,Right,Up,Down};
        private SnakeDirection snakeDirection = SnakeDirection.Right;

        private int snakeLenght;
        private int currentScore=0;

        private DispatcherTimer gameTimer = new DispatcherTimer();

        public ObservableCollection<SnakeHighScore> HighScoreList { get; set; } = new ObservableCollection<SnakeHighScore>();

        private SpeechSynthesizer speechSynthesizer = new SpeechSynthesizer();



        public MainWindow()
        {
            InitializeComponent();
            gameTimer.Tick += GameTimer_Tick;
            LoadHighScoreList();
        }

        private void DrawSnakeFood()
        {
            Point foodPosition = GetNextFoodPosition();
            snakeFood = new Ellipse()
            {
                Fill = foodBrush,
                Width = snakeSquareSize,
                Height = snakeSquareSize,
            };
            gameArea.Children.Add(snakeFood);
            Canvas.SetLeft(snakeFood,foodPosition.X);
            Canvas.SetTop(snakeFood, foodPosition.Y);
        }
        private Point GetNextFoodPosition()
        {
            int maxX = (int)(gameArea.ActualWidth/snakeSquareSize);
            int maxY = (int)(gameArea.ActualHeight/snakeSquareSize);
            int foodX = (random.Next(0,maxX)) * snakeSquareSize;
            int foodY = (random.Next(0, maxY)) * snakeSquareSize;

            foreach (SnakePart snakePart in snakeParts)
            {
                if(snakePart.Position.X == foodX && snakePart.Position.Y == foodY)
                {
                    return GetNextFoodPosition();
                }
            }
            return new Point(foodX, foodY);
        }
        private void StartNewGame()
        {
            bdrWelcomeMessage.Visibility = Visibility.Collapsed;
            bdrHighScoreList.Visibility = Visibility.Collapsed;
            brdEndOfGame.Visibility = Visibility.Collapsed;

            foreach (SnakePart snakePart in snakeParts)
            {
                if(snakePart.UIElement !=null)
                {
                    gameArea.Children.Remove(snakePart.UIElement);
                }
            }
            snakeParts.Clear();
            if(snakeFood != null)
            {
                gameArea.Children.Remove(snakeFood);
            }
            currentScore = 0;
            snakeLenght = snakeStartLenght;
            snakeDirection = SnakeDirection.Right;
            snakeParts.Add(new SnakePart() { Position = new Point(snakeSquareSize * 5, snakeSquareSize * 5) });
            gameTimer.Interval = TimeSpan.FromMilliseconds(snakeStartSpeed);

            DrawSnake();
            DrawSnakeFood();

            UpdateGameStatus();
            gameTimer.IsEnabled = true;
        }

        private void GameTimer_Tick(object sender, EventArgs e)
        {
            DoCollisionCheck();
            MoveSnake();
            DoCollisionCheck();
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            DrawGameArea();
        }
        private void DrawSnake()
        {
            foreach (SnakePart snakePart in snakeParts)
            {
                if(snakePart.UIElement ==null)
                {
                    snakePart.UIElement = new Rectangle()
                    {
                        Height = snakeSquareSize,
                        Width = snakeSquareSize,
                        Fill = snakePart.IsHead ? headColor : bodyColor,
                    };
                    gameArea.Children.Add(snakePart.UIElement);
                    Canvas.SetTop(snakePart.UIElement, snakePart.Position.Y);
                    Canvas.SetLeft(snakePart.UIElement, snakePart.Position.X);
                }
            }
        }

        private void MoveSnake()
        {
            while(snakeParts.Count>=snakeLenght)
            {
                gameArea.Children.Remove(snakeParts[0].UIElement);
                snakeParts.RemoveAt(0);
            }

            foreach (SnakePart snakePart in snakeParts)
            {
                (snakePart.UIElement as Rectangle).Fill = bodyColor;
                snakePart.IsHead = false;
            }

            SnakePart snakeHead = snakeParts[snakeParts.Count - 1];
            double nextX = snakeHead.Position.X;
            double nextY = snakeHead.Position.Y;

            switch(snakeDirection)
            {
                case SnakeDirection.Up:
                    {
                        nextY -= snakeSquareSize;
                        break;
                    }
                case SnakeDirection.Down:
                    {
                        nextY += snakeSquareSize;
                        break;
                    }
                case SnakeDirection.Right:
                    {
                        nextX += snakeSquareSize;
                        break;
                    }
                case SnakeDirection.Left:
                    {
                        nextX -= snakeSquareSize;
                        break;
                    }
            }
            snakeParts.Add(new SnakePart()
            {
                IsHead = true,
                Position = new Point(nextX, nextY),
            });

            DrawSnake();

        }

        private void DrawGameArea()
        {
            bool doneDrawingBackground = false;
            int nextX = 0;
            int nextY = 0;
            int rowCounter = 0;
            bool nextIsOdd = false;

            while(doneDrawingBackground==false)
            {
                Rectangle rectangle = new Rectangle()
                {
                    Width = snakeSquareSize,
                    Height = snakeSquareSize,
                    Fill = nextIsOdd ? Brushes.White : Brushes.Black,
                };
                gameArea.Children.Add(rectangle);
                Canvas.SetTop(rectangle, nextY);
                Canvas.SetLeft(rectangle, nextX);

                nextIsOdd = !nextIsOdd;
                nextX += snakeSquareSize;

                if(nextX>=gameArea.ActualWidth)
                {
                    nextX = 0;
                    nextY += snakeSquareSize;
                    rowCounter++;
                    nextIsOdd = (rowCounter % 2 != 0);
                }

                if(nextY>=gameArea.ActualHeight)
                {
                    doneDrawingBackground = true;
                }
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            SnakeDirection originalSnakeDirection = snakeDirection;
            switch(e.Key)
            {
                case Key.Up:
                    {
                        if(snakeDirection != SnakeDirection.Down)
                            snakeDirection = SnakeDirection.Up;
                        break;
                    }
                case Key.Left:
                    {
                        if(snakeDirection != SnakeDirection.Right)
                            snakeDirection = SnakeDirection.Left;
                        break;
                    }
                case Key.Down:
                    {
                        if(snakeDirection !=SnakeDirection.Up)
                            snakeDirection = SnakeDirection.Down;
                        break;
                    }
                case Key.Right:
                    {
                        if (snakeDirection != SnakeDirection.Left)
                            snakeDirection = SnakeDirection.Right;
                        break;
                    }
                case Key.Space:
                    {
                        StartNewGame();
                        break;
                    }
                case Key.Escape:
                    {
                        this.Close();
                        break;
                    }
            }
            if (snakeDirection != originalSnakeDirection)
                MoveSnake();

        }

        private void DoCollisionCheck()
        {
            SnakePart snakeHead = snakeParts[snakeParts.Count-1];
            if(snakeHead.Position.X < 0 || snakeHead.Position.X >= gameArea.ActualWidth || snakeHead.Position.Y < 0 || snakeHead.Position.Y>=gameArea.ActualHeight)
            {
                EndGame();
            }
            if(snakeHead.Position.X == Canvas.GetLeft(snakeFood) && snakeHead.Position.Y == Canvas.GetTop(snakeFood))
            {
                EatSnakeFood();
                return;
            }
            foreach (SnakePart bodyPart in snakeParts.Take(snakeParts.Count-1))
            {
                if((snakeHead.Position.X == bodyPart.Position.X) && (snakeHead.Position.Y==bodyPart.Position.Y))
                {
                    EndGame();
                }
            }
        }

        private void LoadHighScoreList()
        {
            if(File.Exists("snake_highscorelist.xml"))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<SnakeHighScore>));
                using (Stream reader = new FileStream("snake_highscorelist.xml", FileMode.Open))
                {
                    List<SnakeHighScore> tempList = (List<SnakeHighScore>)serializer.Deserialize(reader);
                    this.HighScoreList.Clear();
                    foreach (var item in tempList.OrderByDescending(x=>x.Score))
                    {
                        this.HighScoreList.Add(item);
                    }
                }
            }

        }

        private void SaveHighScoreList()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ObservableCollection<SnakeHighScore>));
            using (Stream writer = new FileStream("snake_highscorelist.xml", FileMode.Create))
            {
                serializer.Serialize(writer, this.HighScoreList);
            }
        }

        private void EatSnakeFood()
        {
            snakeLenght++;
            currentScore++;
            int timerInterval = Math.Max(snakeSpeedThreshold, (int)gameTimer.Interval.TotalMilliseconds - (currentScore * 2));
            gameTimer.Interval = TimeSpan.FromMilliseconds(timerInterval);
            gameArea.Children.Remove(snakeFood);
            speechSynthesizer.SpeakAsync("mniam");
            DrawSnakeFood();
            UpdateGameStatus();
        }

        private void UpdateGameStatus()
        {
            tbStatusScore.Text = currentScore.ToString();
            tbStatusSpeed.Text = gameTimer.Interval.TotalMilliseconds.ToString();

        }

        private void EndGame()
        {
            bool isNewHighScore = false;
            if(currentScore>0)
            {
                int lowestHighScore = HighScoreList.Count > 0 ? HighScoreList.Min(x => x.Score) : 0;
                if(currentScore>lowestHighScore || HighScoreList.Count < MaxHighScoreListEntryCount)
                {
                    brdNewHighScore.Visibility = Visibility.Visible;
                    txtPlayerName.Focus();
                    isNewHighScore = true;
                    SpeakEndOfTheGameInfo(isNewHighScore);
                }
            }
            if(!isNewHighScore)
            {
                tbFinalScore.Text = currentScore.ToString();
                SpeakEndOfTheGameInfo(!isNewHighScore);
                brdEndOfGame.Visibility = Visibility.Visible;
            }
 
            gameTimer.IsEnabled = false;

        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnShowHighscoreList_Click(object sender, RoutedEventArgs e)
        {
            bdrWelcomeMessage.Visibility = Visibility.Collapsed;
            bdrHighScoreList.Visibility = Visibility.Visible;
        }

        private void btnAddToHighScoreList_Click(object sender, RoutedEventArgs e)
        {
            int newIndex = 0;

            if((this.HighScoreList.Count>0)&&(currentScore<this.HighScoreList.Max(x=>x.Score)))
            {
                SnakeHighScore justAbove = this.HighScoreList.OrderByDescending(x => x.Score).First(x => x.Score >= currentScore);
                if(justAbove !=null)
                {
                    newIndex = this.HighScoreList.IndexOf(justAbove) + 1;
                }
            }

            this.HighScoreList.Insert(newIndex, new SnakeHighScore()
            {
                PlayerName = txtPlayerName.Text,
                Score = currentScore,
            });

            while(this.HighScoreList.Count > MaxHighScoreListEntryCount)
            {
                this.HighScoreList.RemoveAt(MaxHighScoreListEntryCount);
            }

            SaveHighScoreList();
            brdNewHighScore.Visibility = Visibility.Collapsed;
            bdrHighScoreList.Visibility = Visibility.Visible;

        }

        private void SpeakEndOfTheGameInfo(bool isNewHighScore)
        {
            PromptBuilder promptBuilder = new PromptBuilder();

            promptBuilder.StartStyle(new PromptStyle()
            {
                Emphasis = PromptEmphasis.Reduced,
                Volume = PromptVolume.ExtraLoud,
                Rate = PromptRate.Medium,
            });
            promptBuilder.AppendText("o nie!");
            promptBuilder.AppendBreak(TimeSpan.FromMilliseconds(200));
            promptBuilder.AppendText("Przegrałeś?!");
            promptBuilder.EndStyle();

            if(isNewHighScore)
            {
                promptBuilder.AppendBreak(TimeSpan.FromMilliseconds(200));
                promptBuilder.StartStyle(new PromptStyle()
                {
                    Emphasis = PromptEmphasis.Reduced,
                    Volume = PromptVolume.ExtraLoud,
                    Rate = PromptRate.Medium,
                });
                promptBuilder.AppendText("Twój wynik to");
                promptBuilder.AppendBreak(TimeSpan.FromMilliseconds(100));
                promptBuilder.AppendTextWithHint(currentScore.ToString(), SayAs.NumberCardinal);
                promptBuilder.EndStyle();
            }
            speechSynthesizer.SpeakAsync(promptBuilder);
        }


    }
}
