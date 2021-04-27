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
        private static readonly BitmapImage Scissors = new(new Uri("scissors.png", UriKind.Relative));
        private static readonly BitmapImage Paper = new(new Uri("paper.png", UriKind.Relative));
        private static readonly BitmapImage Rock = new(new Uri("rock.png", UriKind.Relative));


        private int _youScore = 0;

        private int YouScore
        {
            get => _youScore;
            set
            {
                Dispatcher.Invoke(() =>
                {
                    if (value > _youScore)
                    {
                        _winner = 1;
                    }

                    _youScore = value;
                    ScoreBlock.Text = $"You: {YouScore}, Enemy: {EnemyScore}, Max: {_maxScore}";
                });
            }
        }

        private int _enemyScore = 0;

        private int EnemyScore
        {
            get => _enemyScore;
            set
            {
                Dispatcher.Invoke(() =>
                {
                    if (value > _enemyScore)
                    {
                        _winner = 2;
                    }

                    _enemyScore = value;
                    ScoreBlock.Text = $"You: {YouScore}, Enemy: {EnemyScore}, Max: {_maxScore}";
                });
            }
        }

        private readonly int _maxScore;
        private string _enemyName;
        private string _youName;

        private readonly bool _host;

        private bool _youMoved = false;
        private bool _enemyMoved = false;

        private Move _youMoveBuffer;
        private Move _enemyMoveBuffer;
        private string GameName { get; init; }

        private int _winner;

        public GameWindow(bool host, string name, string secondPlayer, int maxScore)
        {
            InitializeComponent();

            GameName = name;
            _host = host;
            _enemyName = secondPlayer;
            _maxScore = maxScore;
        }

        private async void Play()
        {
            await Task.Run(() =>
            {
                while (YouScore < _maxScore && EnemyScore < _maxScore)
                {
                    Dispatcher.Invoke(() =>
                        {
                            EnemyReadyBlock.Text = $"{_enemyName}: thinking...";
                            YouReadyBlock.Text = $"{_youName}: thinking...";
                            EnemyMove.Text = string.Empty;
                            EnemyImage.Source = null;
                            YouImage.Source = null;
                        }
                    );

                    EnableButtons();

                    var basePacket = ConnectionManager.Receive();
                    if (basePacket is MovePacket move)
                    {
                        _enemyMoveBuffer = move.Move;
                        _enemyMoved = true;

                        Move(_enemyMoveBuffer, false);
                    }
                    else if (basePacket is Surrender)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            EnemyReadyBlock.Text = $"{_enemyName}: SURRENDERED!!!";
                            YouReadyBlock.Text = $"{ConnectionManager.ClientName}: WON THE GAME!!!";
                        });

                        if (_host) ConnectionManager.Send(new CloseGamePacket(GameName));
                        return;
                    }
                    else
                    {
                        throw new Exception("Not expected packet type");
                    }

                    while (!_youMoved)
                    {
                    }

                    _winner = 0;

                    switch (_youMoveBuffer)
                    {
                        case NetLib.Move.Rock:
                            if (_enemyMoveBuffer == NetLib.Move.Paper) EnemyScore++;
                            else if (_enemyMoveBuffer == NetLib.Move.Scissors) YouScore++;
                            break;
                        case NetLib.Move.Paper:
                            if (_enemyMoveBuffer == NetLib.Move.Rock) YouScore++;
                            else if (_enemyMoveBuffer == NetLib.Move.Scissors) EnemyScore++;
                            break;
                        case NetLib.Move.Scissors:
                            if (_enemyMoveBuffer == NetLib.Move.Paper) YouScore++;
                            else if (_enemyMoveBuffer == NetLib.Move.Rock) EnemyScore++;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    Dispatcher.Invoke(() =>
                    {
                        if (_winner == 0)
                        {
                            EnemyReadyBlock.Text = $"{_enemyName}: Draw";
                            YouReadyBlock.Text = $"{ConnectionManager.ClientName}: Draw";
                        }
                        else if (_winner == 1)
                        {
                            EnemyReadyBlock.Text = $"{_enemyName}: lost round";
                            YouReadyBlock.Text = $"{ConnectionManager.ClientName}: won the round";
                        }
                        else if (_winner == 2)
                        {
                            EnemyReadyBlock.Text = $"{_enemyName}: won the round";
                            YouReadyBlock.Text = $"{ConnectionManager.ClientName}: lost round";
                        }
                    });


                    Thread.Sleep(3000);

                    _youMoved = false;
                    _enemyMoved = false;
                }


                Dispatcher.Invoke(() =>
                {
                    if (YouScore > EnemyScore)
                    {
                        EnemyReadyBlock.Text = $"{_enemyName}: LOST THE GAME!!!";
                        YouReadyBlock.Text = $"{ConnectionManager.ClientName}: WON THE GAME!!!";
                    }
                    else
                    {
                        EnemyReadyBlock.Text = $"{_enemyName}: WON THE GAME!!!";
                        YouReadyBlock.Text = $"{ConnectionManager.ClientName}: LOST THE GAME!!!";
                    }

                });

                if (_host) ConnectionManager.Send(new CloseGamePacket(GameName));
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
                    YouReadyBlock.Text = $"{_youName}: Ready";
                    _youMoveBuffer = move;
                });

                ConnectionManager.Send(new MovePacket(move, GameName, _host));
            }
            else
            {
                Dispatcher.Invoke(() => { EnemyReadyBlock.Text = $"{_enemyName}: Ready"; });
            }

            if (!you && !_youMoved) return;

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
                _youMoved = true;


                if (_enemyMoved)
                    Move(_enemyMoveBuffer, false);

                DisableButtons();
            }
            else
            {
                _enemyMoved = true;
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

            _youName = ConnectionManager.ClientName;

            if (_host)
            {
                if (ConnectionManager.Receive() is PlayerPacket player)
                {
                    _enemyName = player.Name;
                }
            }

            Play();
        }

        private void GameWindow_OnClosed(object? sender, EventArgs e)
        {
            ConnectionManager.Send(new Surrender());
            if (_host) ConnectionManager.Send(new CloseGamePacket(GameName));
        }
    }
}
