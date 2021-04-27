using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using NetLib;

namespace Client
{
    /// <summary>
    /// Interaction logic for GameWindow.xaml
    /// </summary>
    public partial class GameWindow : Window
    {
        private static BitmapImage Scissors = new(new Uri("scissors.png", UriKind.Relative));
        private static BitmapImage Paper = new(new Uri("paper.png", UriKind.Relative));
        private static BitmapImage Rock = new(new Uri("rock.png", UriKind.Relative));


        private int youScore = 0;
        private int YouScore
        {
            get => youScore;
            set
            {
                Dispatcher.Invoke(() =>
                {
                    if (value > youScore)
                    {
                        winner = 1;
                    }
                    youScore = value;
                    ScoreBlock.Text = $"You: {YouScore}, Enemy: {EnemyScore}, Max: {MaxScore}";
                });
            }
        }
        private int enemyScore = 0;
        private int EnemyScore
        {
            get => enemyScore;
            set
            {
                Dispatcher.Invoke(() =>
                {
                    if (value > enemyScore)
                    {
                        winner = 2;
                    }
                    enemyScore = value;
                    ScoreBlock.Text = $"You: {YouScore}, Enemy: {EnemyScore}, Max: {MaxScore}";
                });
            }
        }

        private int MaxScore;
        private string EnemyName;
        private string YouName;

        private bool Host;

        private bool YouMoved = false;
        private bool EnemyMoved = false;

        private Move YouMoveBuffer;
        private Move EnemyMoveBuffer;
        private string GameName { get; init; }

        private int winner;
        public GameWindow(bool host, string name, string secondPlayer, int maxScore)
        {
            InitializeComponent();

            GameName = name;
            Host = host;
            EnemyName = secondPlayer;
            MaxScore = maxScore;
        }

        private async void Play()
        {
            await Task.Run(() =>
            {
                while (YouScore < MaxScore && EnemyScore < MaxScore)
                {
                    Dispatcher.Invoke(() =>
                        {
                            EnemyReadyBlock.Text = $"{EnemyName}: thinking...";
                            YouReadyBlock.Text = $"{YouName}: thinking...";
                            EnemyMove.Text = String.Empty;
                            EnemyImage.Source = null;
                            YouImage.Source = null;
                        }
                    );
                    
                    EnableButtons();

                    if (ConnectionManager.Receive() is MovePacket move)
                    {
                        EnemyMoveBuffer = move.Move;
                        EnemyMoved = true;
                        
                        Move(EnemyMoveBuffer, false);
                    }
                    else
                    {
                        throw new Exception("Not expected packet type");
                    }

                    while (!YouMoved)
                    {
                    }

                    winner = 0;
                    
                    switch (YouMoveBuffer)
                    {
                        case NetLib.Move.Rock:
                            if (EnemyMoveBuffer == NetLib.Move.Paper) EnemyScore++;
                            else if (EnemyMoveBuffer == NetLib.Move.Scissors) YouScore++;
                            break;
                        case NetLib.Move.Paper:
                            if (EnemyMoveBuffer == NetLib.Move.Rock) YouScore++;
                            else if (EnemyMoveBuffer == NetLib.Move.Scissors) EnemyScore++;
                            break;
                        case NetLib.Move.Scissors:
                            if (EnemyMoveBuffer == NetLib.Move.Paper) YouScore++;
                            else if (EnemyMoveBuffer == NetLib.Move.Rock) EnemyScore++;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    Dispatcher.Invoke(() =>
                    {
                        if (winner == 0)
                        {
                            EnemyReadyBlock.Text = $"{EnemyName}: Draw";
                            YouReadyBlock.Text = $"{ConnectionManager.ClientName}: Draw";
                        }
                        else if (winner == 1)
                        {
                            EnemyReadyBlock.Text = $"{EnemyName}: lost round";
                            YouReadyBlock.Text = $"{ConnectionManager.ClientName}: won the round";
                        }
                        else if (winner == 2)
                        {
                            EnemyReadyBlock.Text = $"{EnemyName}: won the round";
                            YouReadyBlock.Text = $"{ConnectionManager.ClientName}: lost round";
                        }
                    });
                    

                    Thread.Sleep(3000);

                    YouMoved = false;
                    EnemyMoved = false;
                }


                Dispatcher.Invoke(() =>
                {
                    if (YouScore > EnemyScore)
                    {
                        EnemyReadyBlock.Text = $"{EnemyName}: LOST THE GAME!!!";
                        YouReadyBlock.Text = $"{ConnectionManager.ClientName}: WON THE GAME!!!";
                    }
                    else
                    {
                        EnemyReadyBlock.Text = $"{EnemyName}: WON THE GAME!!!";
                        YouReadyBlock.Text = $"{ConnectionManager.ClientName}: LOST THE GAME!!!";
                    }
                    
                });
                
                if(Host) ConnectionManager.Send(new CloseGamePacket(GameName));
            });
                        
        }

        private void RockButton_OnClick(object sender, RoutedEventArgs e)
        {
            Move(NetLib.Move.Rock);
        }

        private void PaperButton_OnClick(object sender, RoutedEventArgs e)
        {
            Move(NetLib.Move.Paper);
        }

        private void ScissorsButton_OnClick(object sender, RoutedEventArgs e)
        {
            Move(NetLib.Move.Scissors);
        }
        
        private void Move(Move move, bool you = true)
        {
            if (you)
            {
                Dispatcher.Invoke(() =>
                {
                    YouReadyBlock.Text = $"{YouName}: Ready";
                    YouMoveBuffer = move;
                });

                ConnectionManager.Send(new MovePacket(move, GameName, Host));
            }
            else
            {
                Dispatcher.Invoke(() =>
                {
                    EnemyReadyBlock.Text = $"{EnemyName}: Ready";
                });
            }
            
            if(!you && !YouMoved) return;

            Dispatcher.Invoke(() =>
            {
                switch (move)
                {
                    case NetLib.Move.Rock:
                        if (you) YouImage.Source = Rock;
                        else
                        {
                            EnemyImage.Source = Rock;
                            EnemyMove.Text = "Rock";
                        }
                        break;
                    case NetLib.Move.Paper:
                        if (you) YouImage.Source = Paper;
                        else
                        {
                            EnemyImage.Source = Paper;
                            EnemyMove.Text = "Paper";
                        }
                        break;
                    case NetLib.Move.Scissors:
                        if (you) YouImage.Source = Scissors;
                        else
                        {
                            EnemyImage.Source = Scissors;
                            EnemyMove.Text = "Scissors";
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(move), move, null);
                }
            });


            if (you)
            {
                YouMoved = true;

                
                if(EnemyMoved)
                    Move(EnemyMoveBuffer, false);
                
                DisableButtons();
            }
            else
            {
                EnemyMoved = true;
            }
        }

        private void DisableButtons()
        {
            Dispatcher.Invoke(() =>
            {
                RockButton.IsEnabled = false;
                PaperButton.IsEnabled = false;
                ScissorsButton.IsEnabled = false;
            });
        }

        private void EnableButtons()
        {
            Dispatcher.Invoke(() =>
            {
                RockButton.IsEnabled = true;
                PaperButton.IsEnabled = true;
                ScissorsButton.IsEnabled = true;
            });
            
        }

        private void GameWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            DisableButtons();

            YouName = ConnectionManager.ClientName;

            if (Host)
            {
                if (ConnectionManager.Receive() is PlayerPacket player)
                {
                    EnemyName = player.Name;
                }
            }

            Play();
        }
    }
}
