using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Input;
using System.IO;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Generic;

namespace UtilityPackage
{
    public partial class MainWindow : Window
    {
        bool sldrDragStart = false;
        bool state_play = false;
        string currentfilename = null;
        // ----------------------------- 재생목록 -----------------------------
        public class media
        {
            public string filename { get; set; }
            public string path { get; set; }
            public string bookmark { get; set; }
        }
        bool PlayListOnOff = false;             // playList On/Off 변수
        bool ConfigOnOff = false;             // Config On/Off 변수
        bool MiniModeOnOff = false;             // Config On/Off 변수
        bool FullScreenModeOnOff = false;             // Config On/Off 변수
        ObservableCollection<media> _item = new ObservableCollection<media>();
        public ObservableCollection<media> item { get { return _item; } }
        // --------------------------------------------------------------------
        public MainWindow()
        {
            InitializeComponent();

            this.MouseLeftButtonDown += MovingEveryWhere;

            StreamReader sr = new StreamReader(new FileStream("save.txt", FileMode.OpenOrCreate));
            string str1, str2;
            while ((str1 = sr.ReadLine()) != null)
            {
                str2 = sr.ReadLine();
                _item.Add(new media
                {
                    filename = str1,
                    path = str2
                });
            }
            sr.Close();
        }

        void MovingEveryWhere(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void MediaMain_MediaOpened(object sender, RoutedEventArgs e)
        { // 미디어 파일이 열리면, 플레이타임 슬라이더의 값을 초기화 한다. 
            sldrPlayTime.Minimum = 0;
            sldrPlayTime.Maximum = mediaMain.NaturalDuration.TimeSpan.TotalSeconds;
        }

        private void MediaMain_MediaEnded(object sender, RoutedEventArgs e)
        { // 미디어 중지 
            mediaMain.Stop();

        }
        private void MediaMain_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        { // 미디어 파일 실행 오류시 
            MessageBox.Show("동영상 재생 실패 : " + e.ErrorException.Message.ToString());
        }

        private void BtnSelectFile_Click(object sender, RoutedEventArgs e)
        { // Win32 DLL 을 사용하여 선택할 파일 다이얼로그를 실행한다.
            OpenFileDialog dlg = new OpenFileDialog()
            {
                DefaultExt = ".avi",
                Filter = "All files (*.*)|*.*",
                Multiselect = false
            };
            dlg.Filter = "(mp3,wav,mp4,mov,wmv,mpg,avi,3gp,flv)|*.mp3;*.wav;*.mp4;*.3gp;*.avi;*.mov;*.flv;*.wmv;*.mpg|all files|*.*";
            if (dlg.ShowDialog() == true)
            { // 선택한 파일을 Media Element에 지정하고 초기화한다. 
                mediaMain.Source = new Uri(dlg.FileName); mediaMain.Volume = 0.5;
                mediaMain.SpeedRatio = 1; // 동영상 파일의 Timespan 제어를 위해 초기화와 이벤트처리기를 추가한다. 
                DispatcherTimer timer = new DispatcherTimer()
                { Interval = TimeSpan.FromSeconds(1) };
                timer.Tick += TimerTickHandler; timer.Start(); // 선택한 파일을 실행
                mediaMain.Play();
                // ---------------------------------- 재생목록 ----------------------------------
                media item = _item.Where(z => z.path == dlg.FileName).FirstOrDefault();
                if (item == null)
                {
                    _item.Add(new media
                    {
                        filename = dlg.SafeFileName,
                        path = dlg.FileName
                    });

                    StreamWriter sw = new StreamWriter(new FileStream("save.txt", FileMode.Append));
                    sw.WriteLine(dlg.SafeFileName);
                    sw.WriteLine(dlg.FileName);
                    sw.Close();

                    currentfilename = dlg.SafeFileName.ToString();
                }
                else
                {
                    MessageBox.Show("재생목록에 있습니다.");
                    currentfilename = item.filename.ToString();
                }
                CurrentLabel.Content = "재생 : " + currentfilename;
                // --------------------------------------------------------------------------------
            }
        } // 미디어파일 타임 핸들러 // 미디어파일의 실행시간이 변경되면 호출된다.


        void TimerTickHandler(object sender, EventArgs e)
        { // 미디어파일 실행시간이 변경되었을 때 사용자가 임의로 변경하는 중인지를 체크한다. 
            if (sldrDragStart) return;
            if (mediaMain.Source == null || !mediaMain.NaturalDuration.HasTimeSpan)
            {
                lblPlayTime.Content = "No file selected..."; return;
            }
            // 미디어 파일 총 시간을 슬라이더와 동기화한다.
            sldrPlayTime.Value = mediaMain.Position.TotalSeconds;
        }
        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (mediaMain.Source == null) return; mediaMain.Play();
            CurrentLabel.Content = "재생 : " + currentfilename;
        }
        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            if (mediaMain.Source == null) return; mediaMain.Stop();
            CurrentLabel.Content = "정지 : " + currentfilename;
        }
        private void BtnPause_Click(object sender, RoutedEventArgs e)
        {
            if (mediaMain.Source == null) return; mediaMain.Pause();
            CurrentLabel.Content = "일시정지 : " + currentfilename;
        }
        private void BtnMute_Click(object sender, RoutedEventArgs e)
        {

            if (sldrVolume.Value != 0)
                sldrVolume.Value = 0;
            else
                sldrVolume.Value = 100;
        }
        private void ChangeMediaVolume(object sender, RoutedPropertyChangedEventArgs<double> args)
        {
            if (mediaMain != null)
                mediaMain.Volume = (double)sldrVolume.Value;
            if (soundLabel != null)
                soundLabel.Content = (mediaMain.Volume * 100).ToString("F0") + "%";
        }

        private void SldrPlayTime_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        { // 사용자가 시간대를 변경하면, 잠시 미디어 재생을 멈춘다.
            sldrDragStart = true;
            mediaMain.Pause();
        }
        private void SldrPlayTime_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        { // 사용자가 지정한 시간대로 이동하면, 이동한 시간대로 값을 지정한다.
            mediaMain.Position = TimeSpan.FromSeconds(sldrPlayTime.Value); // 멈췄던 미디어를 재실행한다
            mediaMain.Play();
            sldrDragStart = false;
        }
        private void SldrPlayTime_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (mediaMain.Source == null) return; // 플레이시간이 변경되면, 표시영역을 업데이트한다. 
            lblPlayTime.Content = String.Format("{0} / {1}", mediaMain.Position.ToString(@"mm\:ss"), mediaMain.NaturalDuration.TimeSpan.ToString(@"mm\:ss"));

            // ----------------------------- 클릭으로 장면 이동 ----------------------------- 
            mediaMain.Position = TimeSpan.FromSeconds(sldrPlayTime.Value);      //xaml의 slider에 IsMoveToPointEnabled="True" 속성 추가
            // ------------------------------------------------------------------------------ 
        }

        // ---------------------------------------- 화면 이동 단축키 --------------------------------------------
        private void play_L_10(object sender, RoutedEventArgs e)
        {
            mediaMain.Position = TimeSpan.FromSeconds(sldrPlayTime.Value - 10.0);
        }

        private void play_R_10(object sender, RoutedEventArgs e)
        {
            mediaMain.Position = TimeSpan.FromSeconds(sldrPlayTime.Value + 10.0);
        }

        // ---------------------------------------- 볼륨 조절 단축키 --------------------------------------------
        private void volume_U_10(object sender, RoutedEventArgs e)
        {
            sldrVolume.Value += 0.1;
        }
        private void volume_D_10(object sender, RoutedEventArgs e)
        {
            sldrVolume.Value -= 0.1;
        }
        // ------------------------------------------------------------------------------------------------------
        private void play_Space(object sender, RoutedEventArgs e)
        {
            if (state_play == true) { mediaMain.Pause(); state_play = false; CurrentLabel.Content = "일시정지 : " + currentfilename;}
            else { mediaMain.Play(); state_play = true; CurrentLabel.Content = "재생 : " + currentfilename; }
        }
        private void ChangeMediaSpeed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (mediaMain != null)
                mediaMain.SpeedRatio = (double)sldrSpeedRatio.Value;
            if (speedLabel != null)
                speedLabel.Content = sldrSpeedRatio.Value.ToString("F1") + "x";
        }
        private void mainwindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down)
                volume_D_10(sender, e);
            if (e.Key == Key.Up)
                volume_U_10(sender, e);
            if (e.Key == Key.Left)
                play_L_10(sender, e);
            if (e.Key == Key.Right)
                play_R_10(sender, e);

            if (e.Key == Key.Escape)
                btnExit_Click(sender, e);
            if (e.Key == Key.P)
                play_Space(sender, e);
            if (e.Key == Key.L)
                Btnplaylist_Click(sender, e);

            if (e.Key == Key.O && Keyboard.IsKeyDown(Key.LeftCtrl))
                BtnSelectFile_Click(sender, e);
            if (e.Key == Key.O)
                openconfig_Click(sender, e);

            if (e.Key == Key.M)
                BtnMute_Click(sender, e);
            if (e.Key == Key.S)
                BtnStop_Click(sender, e);

            if (e.Key == Key.Enter)
                FullScreen_btn_Click(sender, e);
            if (e.Key == Key.N && Keyboard.IsKeyDown(Key.LeftShift))
                btnMinimize_Click(sender, e);
            if (e.Key == Key.M && Keyboard.IsKeyDown(Key.LeftShift))
                btnMaximize_Click(sender, e);
            if (e.Key == Key.B && Keyboard.IsKeyDown(Key.LeftShift))
                BtnSound_Click(sender, e);
            if (e.Key == Key.F && Keyboard.IsKeyDown(Key.LeftShift))
                transparent_toggle(sender, e);

            if (e.Key == Key.X)
                ChangeMediaSpeed_Down(sender, e);
            if (e.Key == Key.C)
                ChangeMediaSpeed_Up(sender, e);
            if (e.Key == Key.Z)
                ChangeMediaSpeed_Default(sender, e);

            if (e.Key == Key.H && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                // 도움말
                help_manual(sender, e);

            }
        }

        private void ChangeMediaSpeed_Up(object sender, RoutedEventArgs e)
        {
            sldrSpeedRatio.Value += 0.1;
        }
        private void ChangeMediaSpeed_Down(object sender, RoutedEventArgs e)
        {
            sldrSpeedRatio.Value -= 0.1;
        }
        private void ChangeMediaSpeed_Default(object sender, RoutedEventArgs e)
        {
            sldrSpeedRatio.Value = 1.0;
        }
        private void help_manual(object sender, RoutedEventArgs e)
        {
            string help;
            help = "  화살표 좌우 = 10초 간격으로 이동\n" + "  화살표 상하 = 볼륨 조절\n";
            help += "  ESC = 프로그램 종료\n" + "  P = 일시정지 / 재생\n" + "  L = 재생목록\n";
            help += "  Ctrl O = 파일 열기\n" + "  O = 추가기능 On/Off\n" + "  M = 음소거\n";
            help += "  S = 정지\n" + "  Enter = 전체화면\n" + "  Shift N = 최소화\n" + "  Shift M = 최대화\n" + "  Shift B = 미니모드\n" + "  Shift F = 플로팅 모드\n";
            help += "  Z = 기본 재생 속도\n" + "  X = 재생속도 감소\n" + "  C = 재생속도 증가\n" + "  Ctrl H = 도움말\n";
            MessageBox.Show(help, "도움말");
        }

        //------------------------------------------작성한부분--------------------------------------------------
        /*
         * MainWindow.xaml에서 작성해야하는 부분
         *  1.첫줄에서 Window 옆에                x:Name="mainwindow" DataContext="{Binding RelativeSource={RelativeSource Self}}"
         *  2.세번째줄 mediaelement에서           HorizontalAlignment="Left"
         *  3.네번째줄 sldPlayTime에서            HorizontalAlignment="Left"
         *  4.여덟번째줄 btnSelectFile에서        HorizontalAlignment="left"
         *  5.12 ~ 20번째줄 추가함
         *  
         *  
         *  12-22일자 추가한 것
         *  1. listview dragenter, drop 삭제
         *  2. mainwindow dragenter, drop 추가 (mainwindow 속성에서 allowdrop=true 해야함)
         *  3. listview_PreviewMouseRightButtonDown 추가. 리스트뷰에서 아이템 클릭 후 우클릭하면 삭제됨.
         *  4. 텍스트파일에 저장 추가. 이 글 위아래에 #으로 구간 표시하였음.   경로 프로젝트명\\bin\\debug
         *     위쪽에 표시한 구간은 50번, 103번째 줄임
         */
        private void Btnplaylist_Click(object sender, RoutedEventArgs e)
        {
            if (PlayListOnOff == false) // 재생목록 끄기
            {
                listview.Width = 0;
                mediaMain.Margin = new Thickness(10, 65, 10, 85);
                sldrPlayTime.Margin = new Thickness(10, 0, 10, 42);

                PlayListOnOff = true;
            }
            else if (PlayListOnOff == true) // 재생목록 켜기
            {
                listview.Width = 280;
                mediaMain.Margin = new Thickness(10, 65, 300, 85);
                sldrPlayTime.Margin = new Thickness(10, 0, 300, 42);

                PlayListOnOff = false;
            }
        }
        private void Listview_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var item = listview.SelectedItem as media;
            if (item == null)
                return;
            mediaMain.Source = new Uri(item.path); mediaMain.Volume = 0.5;
            mediaMain.SpeedRatio = 1; // 동영상 파일의 Timespan 제어를 위해 초기화와 이벤트처리기를 추가한다. 
            DispatcherTimer timer = new DispatcherTimer()
            { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += TimerTickHandler; timer.Start(); // 선택한 파일을 실행
            mediaMain.Play();
            currentfilename = item.filename.ToString();
            CurrentLabel.Content = "재생 : " + currentfilename;
        }
        private void Listview_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = listview.SelectedItem as media;
            if (item == null)
                return;
            _item.Remove(item);
            listview.Items.Refresh();
            mediaMain.Stop();
            //#################################################################################################
            StreamReader sr = new StreamReader(new FileStream("save.txt", FileMode.Open));
            List<string> lines = new List<string>();       //using System.Collections.Generic; 선언
            string str;
            while ((str = sr.ReadLine()) != null)
            {
                lines.Add(str);
            }
            sr.Close();
            for (int i = lines.Count - 1; i >= 0; i--)
            {
                if (String.Compare(lines[i], item.filename) == 0)
                {
                    lines.RemoveAt(i);
                    lines.RemoveAt(i);
                }
            }
            StreamWriter sw = new StreamWriter(new FileStream("save.txt", FileMode.Create));
            for (int i = 0; i < lines.Count; i++)
            {
                sw.WriteLine(lines[i]);
            }
            sw.Close();
            //#################################################################################################
        }
        private void Mainwindow_DragEnter(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Copy;
        }
        private void Mainwindow_Drop(object sender, DragEventArgs e)
        {
            StreamWriter sw = new StreamWriter(new FileStream("save.txt", FileMode.Append)); //<<<<<<###############
            String[] File = (String[])e.Data.GetData(DataFormats.FileDrop, true);
            if (File.Length > 0)
            {
                string f_path = File[0].ToString();      //경로땀
                var f_filename = Path.GetFileNameWithoutExtension(f_path);

                media item = _item.Where(z => z.path == f_path).FirstOrDefault();
                if (item == null)
                {
                    //리스트뷰에 아이템 추가
                    _item.Add(new media
                    {
                        filename = f_filename,
                        path = f_path
                    });
                    //텍스트파일에 저장 #################################################################
                    sw.WriteLine(f_filename);
                    sw.WriteLine(f_path);
                    //출력 
                    mediaMain.Source = new Uri(f_path); mediaMain.Volume = 0.5;
                    mediaMain.SpeedRatio = 1; // 동영상 파일의 Timespan 제어를 위해 초기화와 이벤트처리기를 추가한다. 
                    DispatcherTimer timer = new DispatcherTimer()
                    { Interval = TimeSpan.FromSeconds(1) };
                    timer.Tick += TimerTickHandler; timer.Start(); // 선택한 파일을 실행
                    mediaMain.Play();
                }
                else
                    MessageBox.Show("재생목록에 있습니다.");
                currentfilename = f_filename.ToString();
                CurrentLabel.Content = "재생 : " + currentfilename;
            }
            sw.Close();  //<<<<<<<<<<<<#################################
            e.Handled = true;
        }

        private void sldRotateAngle_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            RotateAngle.Angle = sldRotateAngle.Value;
            if (rotateLabel != null)
                rotateLabel.Content = (RotateAngle.Angle).ToString("F0") + "°";
        }

        private void openconfig_Click(object sender, RoutedEventArgs e)
        {
            if (ConfigOnOff == false)
            {
                MainGrid.Margin = new Thickness(0, 0, 0, 45);
                ConfigOnOff = true;
            }
            else if (ConfigOnOff == true)
            {
                MainGrid.Margin = new Thickness(0, 0, 0, 0);
                ConfigOnOff = false;
            }
        }
        private void BtnSound_Click(object sender, RoutedEventArgs e)                                   // 음향만 기능
        {
            if (MiniModeOnOff == false) // 미니모드로 전환
            {
                btnminimode.Content = "영상모드";
                mainwindow.MinHeight = 250;
                mainwindow.MinWidth = 475;
                Height = MinHeight;
                Width = MinWidth;

                PlayListOnOff = true;
                Btnplaylist_Click(sender, e);
                ConfigOnOff = true;
                openconfig_Click(sender, e);

                FullScreen_btn.Visibility = Visibility.Hidden;
                openconfig.Visibility = Visibility.Hidden;
                btnmanual.Visibility = Visibility.Hidden;

                MiniModeOnOff = true;
            }
            else // 영상모드로 전환
            {
                btnminimode.Content = "미니모드";
                mainwindow.MinHeight = 600;
                mainwindow.MinWidth = 850;
                Height = MinHeight;
                Width = MinWidth;

                FullScreen_btn.Visibility = Visibility.Visible;
                openconfig.Visibility = Visibility.Visible;
                btnmanual.Visibility = Visibility.Visible;

                MiniModeOnOff = false;
            }
        }

        private void FullScreen_btn_Click(object sender, RoutedEventArgs e)
        {
            if (FullScreenModeOnOff == false) // 풀스크린 모드로 전환
            {
                mainwindow.WindowStyle = WindowStyle.None;
                mainwindow.WindowState = WindowState.Maximized;

                PlayListOnOff = true;
                Btnplaylist_Click(sender, e);
                ConfigOnOff = true;
                openconfig_Click(sender, e);

                mediaMain.Margin = new Thickness(0, 0, 0, 0);

                FullScreenModeOnOff = true;
            }
            else // 평소모드로 전환
            {
                mainwindow.WindowState = WindowState.Normal;
                mediaMain.Margin = new Thickness(10, 65, 300, 85);
                FullScreenModeOnOff = false;
            }

        }
        private void transparent_toggle(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (transparent.Value >= 1.19)
                transparent.Value = 2.0;
            else
                transparent.Value = 1.8;
        }

        private void transparent_toggle(object sender, RoutedEventArgs e)
        {
            if (transparent.Value == 1.2)
            {
                transparent.Value = 1.1;    transparent_ValueChanged(sender, e);
            }
            else
            {
                transparent.Value = 1.2;    transparent_ValueChanged(sender, e);
            }
        }
        private void transparent_ValueChanged(object sender, RoutedEventArgs e)
        {
            if (transparent.Value < 0.2) // 처음 시작시 정상 작동 하도록
            {
                transparent.Minimum = 0.2;
                transparent.Value = 1.2;
            }
            if (transparent.Value >= 1.19) // 평소모드로 전환
            {
                mainwindow.Background.Opacity = 1;
                mediaMain.Opacity = 1;
                mainwindow.Topmost = false;

                mainwindow.MinWidth = 850;
                mainwindow.MinHeight = 600;

                Height = MinHeight;
                Width = MinWidth;

                FullScreen_btn.Visibility = Visibility.Visible;
                btnminimode.Visibility = Visibility.Visible;
                btnSelectFile.Visibility = Visibility.Visible;
                btnplaylist.Visibility = Visibility.Visible;
                sldrPlayTime.Visibility = Visibility.Visible;
                lblPlayTime.Visibility = Visibility.Visible;
                btnStart.Visibility = Visibility.Visible;
                btnStop.Visibility = Visibility.Visible;
                btnPause.Visibility = Visibility.Visible;
                btnMute.Visibility = Visibility.Visible;
                CurrentLabel.Visibility = Visibility.Visible;
                soundtext.Visibility = Visibility.Visible;
                sldrVolume.Visibility = Visibility.Visible;
                soundLabel.Visibility = Visibility.Visible;
                openconfig.Visibility = Visibility.Visible;
                btnmanual.Visibility = Visibility.Visible;
                btnMinimize.Visibility = Visibility.Visible;
                btnMaximize.Visibility = Visibility.Visible;
                btnExit.Visibility = Visibility.Visible;

                PlayListOnOff = true;
                Btnplaylist_Click(sender, e);

                mediaMain.Margin = new Thickness(10, 65, 300, 85);
            }
            else // 아니라면
            {
                mainwindow.Background.Opacity = 0;
                mediaMain.Opacity = transparent.Value;
                mainwindow.Topmost = true;

                mainwindow.MinWidth = 400;
                mainwindow.MinHeight = 225;

                Height = MinHeight;
                Width = MinWidth;

                FullScreen_btn.Visibility = Visibility.Hidden;
                btnminimode.Visibility = Visibility.Hidden;
                btnSelectFile.Visibility = Visibility.Hidden;
                btnplaylist.Visibility = Visibility.Hidden;
                sldrPlayTime.Visibility = Visibility.Hidden;
                lblPlayTime.Visibility = Visibility.Hidden;
                btnStart.Visibility = Visibility.Hidden;
                btnStop.Visibility = Visibility.Hidden;
                btnPause.Visibility = Visibility.Hidden;
                btnMute.Visibility = Visibility.Hidden;
                CurrentLabel.Visibility = Visibility.Hidden;
                soundtext.Visibility = Visibility.Hidden;
                sldrVolume.Visibility = Visibility.Hidden;
                soundLabel.Visibility = Visibility.Hidden;
                openconfig.Visibility = Visibility.Hidden;
                btnmanual.Visibility = Visibility.Hidden;
                btnMinimize.Visibility = Visibility.Hidden;
                btnMaximize.Visibility = Visibility.Hidden;
                btnExit.Visibility = Visibility.Hidden;


                PlayListOnOff = false;
                Btnplaylist_Click(sender, e);
                ConfigOnOff = true;
                openconfig_Click(sender, e);

                mediaMain.Margin = new Thickness(0, 0, 0, 0);
            }
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnMaximize_Click(object sender, RoutedEventArgs e)
        {   // 최대화
            if (this.WindowState == WindowState.Maximized)
                this.WindowState = WindowState.Normal;
            else
                this.WindowState = WindowState.Maximized;
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {// 최소화
            this.WindowState = WindowState.Minimized;
        }

        //북마크 기능 추가..(미완성)
        //double temp_bookmark;
        //private void savetime(object sender, RoutedEventArgs e) // 현재 진행중인 시각 저장
        //{
        //    temp_bookmark = sldrPlayTime.Value;
        //}
        //private void loadtime(object sender, RoutedEventArgs e) // 저장된 시각으로 영상 맞춤
        //{
        //    sldrPlayTime.Value = temp_bookmark;
        //}
        //private void mainwindow_Closed(object sender, EventArgs e) // 프로그램 종료 시
        //{
        //    StreamWriter sw2 = new StreamWriter(new FileStream("Bookmark.txt", FileMode.Create));
        //    sw2.WriteLine(sldrPlayTime.Value);
        //    sw2.Close();
        //}

        //--------------------------------------끝------------------------------------------------
    }
}

