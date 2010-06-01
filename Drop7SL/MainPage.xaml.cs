using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Resources;
using System.Windows.Media.Imaging;
using System.Threading;
using System.ComponentModel;
using System.Windows.Data;

namespace Drop7
{
    public partial class MainPage : UserControl, INotifyPropertyChanged
    {
        BitmapImage[] BlockImages;
        const int N = 7;
        const int Tile_Size = 43;
        Tile[,] gameBoard = new Tile[N + 1, N];
        Image[,] gameBoardImageRef = new Image[N, N];
        Queue<Image> levelDots = new Queue<Image>();
        Tile NextMove;
        Random RND = new Random();
        bool BusyWorking = false;
        int myLevelPlaysTillFinishLevel = 100, myLevelNumberOfPlays = 100;
        int level;
        public int Level { get
            { return level; }
            set
            {
                if (level != value)
                {
                    level = value;
                    NotifyPropertyChanged("Level");
                }
            }
        }
        int score;
        public int Score {
            get
            {
                return score;
            }
            set
            {
                if (value != score)
                {
                    score = value;
                    NotifyPropertyChanged("Score");
                }
            }
        }
		
        enum Tile
        {
            Empty,
            One,
            Two,
            Three,
            Four,
            Five,
            Six,
            Seven,
            Solid,
            Cracked
        }
		
        public MainPage()
        {
            InitializeComponent();

            BlockImages = new BitmapImage[12];

            for (int i = 0; i < 10; i++)
            {
                StreamResourceInfo sr1 = Application.GetResourceStream(
                new Uri(string.Format("Resources/{0}.png", i), UriKind.Relative));
                BitmapImage bmp1 = new BitmapImage();
                bmp1.SetSource(sr1.Stream);
                BlockImages[i] = bmp1;
            }
            StreamResourceInfo sr = Application.GetResourceStream(new Uri("Resources/por.png", UriKind.Relative));
            BitmapImage bmp = new BitmapImage();
            bmp.SetSource(sr.Stream);
            BlockImages[10] = bmp;

            sr = Application.GetResourceStream(new Uri("Resources/khali.png", UriKind.Relative));
            bmp = new BitmapImage();
            bmp.SetSource(sr.Stream);
            BlockImages[11] = bmp;
            
            Binding b = new Binding("Score");
            b.Source = this;
            ScoreText.SetBinding(TextBlock.TextProperty, b);

            b = new Binding("Level");
            b.Source = this;
            LevelText.SetBinding(TextBlock.TextProperty, b);

            var bw = new BackgroundWorker();
            bw.DoWork += (o,e) =>
            {
                BusyWorking = true;
                Game_New();                
            };
            bw.RunWorkerAsync();
        }

        private void InitializeLevelDots()
        {
            ManualResetEvent h = new ManualResetEvent(false);
            Dispatcher.BeginInvoke(() =>
                {
                    foreach (var item in levelDots)
                    {
                        LevelDotStack.Children.Remove(item);
                    }
                    levelDots.Clear();

                    for (int i = 0; i < myLevelNumberOfPlays; i++)
                    {
                        Image b1 = new Image() { Source = BlockImages[10] };
                        b1.Width = 10;
                        b1.Height = 10;
                        b1.Stretch = Stretch.None;
                        LevelDotStack.Children.Add(b1);
                        levelDots.Enqueue(b1);
                    }
                    h.Set();
                });
            h.WaitOne();
        }

        private void Game_New()
        {
            for (int ii = 0; ii < N + 1; ii++)
                for (int jj = 0; jj < N; jj++)
                    gameBoard[ii, jj] = Tile.Empty;

            for (int ii = 0; ii < N ; ii++)
                for (int jj = 0; jj < N; jj++)
                {
                    if (gameBoardImageRef[ii, jj] != null)
                    {
                        ManualResetEvent h = new ManualResetEvent(false);
                        Dispatcher.BeginInvoke(() => 
                        {
                            BoardGrid.Children.Remove(gameBoardImageRef[ii, jj]);
                            h.Set();
                        });
                        h.WaitOne();
                        gameBoardImageRef[ii, jj] = null;                        
                    }
                }
            

            Game_SetNextMove();

            for (int ii = 0; ii < RND.Next(N / 2, N); ii++)
                Game_Move(RND.Next(N));

            Score = 0;
            Level = 1;
            myLevelNumberOfPlays = 30;
            myLevelPlaysTillFinishLevel = myLevelNumberOfPlays;
            InitializeLevelDots();
            BusyWorking = false;
        }

        private void Game_SetNextMove()
        {
            NextMove = (Tile)RND.Next((int)Tile.One, (int)Tile.Cracked);
            Dispatcher.BeginInvoke(() =>
                {
                    NextMoveImage.Source = BlockImages[(int)NextMove];
                });
        }

        private void DecLevelDots()
        {
            if (levelDots.Count > 0)
            {
                ManualResetEvent wh = new ManualResetEvent(false);
                myLevelPlaysTillFinishLevel--;
                Dispatcher.BeginInvoke(() =>
                    {
                        Image img = levelDots.Dequeue();
                        LevelDotStack.Children.Remove(img);
                        Image b1 = new Image() { Source = BlockImages[11] };
                        b1.Width = 10;
                        b1.Height = 10;
                        b1.Stretch = Stretch.None;
                        LevelDotStack.Children.Add(b1);
                        levelDots.Enqueue(b1);
                        wh.Set();
                    });
                wh.WaitOne();
            }
        }

        private void Game_Move(int Col)
        {
            if (Col >= N || Col < 0)
                return;
            
            int row=N;
            for (; row > 0 && gameBoard[row, Col] != Tile.Empty; row--) ;
            AddBlockToBoard(row, Col, NextMove);

            Game_CheckChain(0);

            DecLevelDots();
            if (myLevelPlaysTillFinishLevel == 0)
            {
                Game_LevelUp();
                Game_CheckChain(0);
            }

            Game_SetNextMove();

            Game_CheckEnd();
        }

        private void Game_LevelUp()
        {
            Level++;
            myLevelNumberOfPlays = Math.Max(6, 31 - Level);
            myLevelPlaysTillFinishLevel = myLevelNumberOfPlays;
            Score += 7000;

            InitializeLevelDots();

            for (int col = 0; col < N; col++)
            {
                for (int row = 1; row < N + 1; row++)
                    if (gameBoard[row, col] != Tile.Empty)
                        MoveUp(row, col);
                AddBlockToBoard(N, col, Tile.Solid);
            }
        }

        private void AddBlockToBoard(int row, int col, Tile tile)
        {
            gameBoard[row, col] = tile;
            if (row > 0)
            {
                ManualResetEvent h = new ManualResetEvent(false);
                Dispatcher.BeginInvoke(() =>
                    {
                        int uiRow = row - 1;
                        Image b1 = new Image() { Source = BlockImages[(int)tile] };
                        b1.Width = 41;
                        b1.Height = 41;
                        b1.Stretch = Stretch.None;
                        b1.SetValue(Grid.ColumnProperty, col);
                        b1.SetValue(Grid.RowProperty, uiRow);

                        if (gameBoardImageRef[uiRow, col] != null)
                        {
                            BoardGrid.Children.Remove(gameBoardImageRef[uiRow, col]);
                            gameBoardImageRef[uiRow, col] = null;
                        }
                        gameBoardImageRef[uiRow, col] = b1;

                        BoardGrid.Children.Add(b1);
                        h.Set();
                    });
                h.WaitOne();
            }
        }

        Storyboard KillSb = null;
        List<Tuple<int, int>> KillList = new List<Tuple<int, int>>();
        private void QueueForKill(int row, int col)
        {
            Dispatcher.BeginInvoke(() =>
                {
                    Image img = gameBoardImageRef[row - 1, col];                    
                    DoubleAnimation anim = new DoubleAnimation() { Duration = new Duration(TimeSpan.FromSeconds(0.4)), To = 0 };
                    Storyboard.SetTarget(anim, img);
                    Storyboard.SetTargetProperty(anim, new PropertyPath(FrameworkElement.OpacityProperty));
                    KillSb.Children.Add(anim);
                });

            gameBoard[row, col] = Tile.Empty;
            KillList.Add(new Tuple<int,int>(row - 1, col));
        }

        private void KillSwitch()
        {
            ManualResetEvent wh = new ManualResetEvent(false);
            Dispatcher.BeginInvoke(() =>
                {
                    KillSb.Completed += (o, e) =>
                    {
                        wh.Set();
                    };
                    KillSb.Begin();
                });
            wh.WaitOne();

            wh.Reset();
            Dispatcher.BeginInvoke(() =>
                {
                    foreach (var item in KillList)
                    {
                        BoardGrid.Children.Remove(gameBoardImageRef[item.Item1, item.Item2]);
                        gameBoardImageRef[item.Item1, item.Item2] = null;
                    }
                    wh.Set();
                });
            wh.WaitOne();
        }

        private void MoveDown(int row, int col)
        {
            ManualResetEvent h = new ManualResetEvent(false);
            Dispatcher.BeginInvoke(() =>
                {
                    Image img = gameBoardImageRef[row - 1, col];
                    img.SetValue(Grid.RowProperty, (int)img.GetValue(Grid.RowProperty) + 1);
                    h.Set();
                });
            h.WaitOne();

            gameBoardImageRef[row, col] = gameBoardImageRef[row - 1, col];
            gameBoardImageRef[row - 1, col] = null;

            gameBoard[row + 1, col] = gameBoard[row, col];
            gameBoard[row, col] = Tile.Empty;
        }

        private void MoveUp(int row, int col)
        {
            if (row > 1)
            {
                ManualResetEvent h = new ManualResetEvent(false);
                Dispatcher.BeginInvoke(() =>
                {
                    Image img = gameBoardImageRef[row - 1, col];
                    img.SetValue(Grid.RowProperty, (int)img.GetValue(Grid.RowProperty) - 1);
                    h.Set();
                });
                h.WaitOne();
                gameBoardImageRef[row - 2, col] = gameBoardImageRef[row - 1, col];
                gameBoardImageRef[row - 1, col] = null;
            }
            else if (row == 1)
            {
                ManualResetEvent h = new ManualResetEvent(false);
                Dispatcher.BeginInvoke(() =>
                {
                    BoardGrid.Children.Remove(gameBoardImageRef[row - 1, col]);
                    h.Set();
                });
                h.WaitOne();
                gameBoardImageRef[row - 1, col] = null;
            }


            gameBoard[row - 1, col] = gameBoard[row, col];
            gameBoard[row, col] = Tile.Empty;
        }

        private void BoardGrid_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            lock (BoardBackImage)
            {
                if (!BusyWorking)
                {
                    BusyWorking = true;
                    int Col = (int)(e.GetPosition(BoardGrid).X * 7 / BoardGrid.Width);
                    e.Handled = true;
                    var bw = new BackgroundWorker();
                    bw.DoWork += (o1, e1) =>
                        {
                            Game_Move(Col);
                        };
                    bw.RunWorkerCompleted += (o2, e2) => { BusyWorking = false; };
                    bw.RunWorkerAsync();
                }
            }
        }

        private void Game_CheckEnd()
        {
            for (int col = 0; col < N; col++)
                if (gameBoard[0, col] != Tile.Empty)
                    Game_End();
        }

        private void Game_End()
        {
            Dispatcher.BeginInvoke(() =>
                {
                    MessageBox.Show("Game Over\nScore: " + Score.ToString(), "Game Over", MessageBoxButton.OK);
                });
            Game_New();
        }

        private void Game_CheckChain(int level)
        {
            Queue<Tuple<int, int>> booms = new Queue<Tuple<int, int>>();

            for (int row = 1; row <= N; row++)
                for (int col = 0; col < N; col++)
                    if (gameBoard[row, col] != Tile.Empty)
                    {
                        int ver = 1, hor = 1;

                        for (int trow = row + 1;
                            trow <= N && gameBoard[trow, col] != Tile.Empty;
                            trow++) ver++;

                        for (int trow = row - 1;
                            trow > 0 && gameBoard[trow, col] != Tile.Empty;
                            trow--) ver++;

                        for (int tcol = col + 1;
                            tcol < N && gameBoard[row, tcol] != Tile.Empty;
                            tcol++) hor++;

                        for (int tcol = col - 1;
                            tcol >= 0 && gameBoard[row, tcol] != Tile.Empty;
                            tcol--) hor++;

                        if (ver == (int)gameBoard[row, col] || hor == (int)gameBoard[row, col])
                            booms.Enqueue(new Tuple<int, int>(row, col));
                    }

            ManualResetEvent h = new ManualResetEvent(false);
            Dispatcher.BeginInvoke(() =>
                {
                    KillSb = new Storyboard();
                    h.Set();
                });
            h.WaitOne();
            KillList.Clear();
            foreach (var b in booms)
            {
                QueueForKill(b.Item1, b.Item2);

                if (b.Item1 + 1 <= N)
                    UpdateUnnumberedBlocks(b.Item1 + 1, b.Item2);

                if (b.Item1 - 1 >= 0)
                    UpdateUnnumberedBlocks(b.Item1 - 1, b.Item2);

                if (b.Item2 + 1 < N)
                    UpdateUnnumberedBlocks(b.Item1, b.Item2 + 1);

                if (b.Item2 - 1 >= 0)
                    UpdateUnnumberedBlocks(b.Item1, b.Item2 - 1);

            }

            KillSwitch();

            for (bool changed = true; changed; )
            {
                changed = false;
                for (int row = N - 1; row > 0; row--)
                    for (int col = 0; col < N; col++)
                        if (gameBoard[row, col] != Tile.Empty)
                            if (gameBoard[row + 1, col] == Tile.Empty)
                            {
                                changed = true;
                                MoveDown(row, col);
                            }
            }

            Score += GetScore(level) * booms.Count;

            if (booms.Count > 0)
            {
                Game_CheckChain(level + 1);
            }
        }

        private int GetScore(int level)
        {
            int S = 0;
            int lc = 1;
            int[] coef = new int[] { 7, 72, -73, 36 };
            for (int ii = 0; ii < 4; ii++)
            {
                S += lc * coef[ii];
                lc *= (level + 1);
            }
            S /= 6;

            return S;
        }

        private Tile Game_GetNumberedTile()
        {
            return (Tile)RND.Next((int)Tile.One, (int)Tile.Solid);
        }

        private void UpdateUnnumberedBlocks(int row, int col)
        {
            if (gameBoard[row, col] == Tile.Solid)
                AddBlockToBoard(row, col, Tile.Cracked);
            else if (gameBoard[row, col] == Tile.Cracked)
                AddBlockToBoard(row, col, Game_GetNumberedTile());
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                Dispatcher.BeginInvoke(() =>
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs(info));
                    });
            }
        }
    }
}
