using System;
using System.Drawing;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Collections.Generic;
using System.IO;
using NAudio.Wave;
using System.Text;
using static Timeline.Form1;

namespace Timeline
{
    public partial class Form1 : Form
    {
        private Timeline _timeline;
        private AudioPlayer _audioPlayer;
        private System.Windows.Forms.Timer _playbackTimer;
        private List<TimelineObject> timelineObjects = new List<TimelineObject>();
        private TimeSpan _currentPlaybackTime; //　現在の再生時間を保持する
        private bool _isPlaying; //　再生状態を示す
        private TimelineObject _selectedObject; //　選択されているオブジェクトの保持する
        private Point _dragStartPoint; //　ドラッグの開始位置を保持する
        private bool _isDragging; //　ドラッグ操作状態を示す
        private int numberOfLayers = 100; // レイヤー数の初期値
        private int pixelsPerMillisecond = 1; // 1ミリ秒あたりのピクセル数（時間軸のスケール）
        private int layerHeight = 50; // 各レイヤーの高さ

        //　初期化
        public Form1()
        {
            InitializeComponent();

            _timeline = new Timeline();
            _audioPlayer = new AudioPlayer();
            
            _playbackTimer = new System.Windows.Forms.Timer();
            _playbackTimer.Interval = 100; // ミリ秒単位、ここでは100msごとに更新
            _playbackTimer.Tick += PlaybackTimer_Tick;

            // TrackBar の初期設定
            UpdateTrackBar(TimeSpan.Zero, TimeSpan.FromSeconds(10)); // 10秒のタイムライン

            // ドラッグ＆ドロップの設定
            this.AllowDrop = true;
            this.DragEnter += Form1_DragEnter;
            this.DragDrop += Form1_DragDrop;

            //オブジェクトのドラッグ移動
            panel1.MouseDown += TimelinePanel_MouseDown;
            panel1.MouseMove += TimelinePanel_MouseMove;
            panel1.MouseUp += TimelinePanel_MouseUp;
        }

        //　Timelineを描画する
        private void Timeline_panel1(object sender, PaintEventArgs e)
        {
            // タイムラインオブジェクトを描画する処理
            int scrollOffset = hScrollBar1.Value;
            foreach (var obj in _timeline.GetObjects())
            {
                DrawTimelineObject(e.Graphics, obj, scrollOffset);
            }

            Graphics g = e.Graphics;

            // レイヤー線を描画する
            DrawLayerLines(g, numberOfLayers);
        }

        //　オブジェクトをタイムライン上に描画する
        private void DrawTimelineObject(Graphics g, TimelineObject obj, int scrollOffset)
        {
            // オブジェクトの位置とサイズを計算して描画
            int x = (int)(obj.StartTime.TotalMilliseconds * pixelsPerMillisecond) - scrollOffset;  // ミリ秒をピクセルに変換
            int width = (int)(obj.Duration.TotalMilliseconds * pixelsPerMillisecond);  // ミリ秒をピクセルに変換
            int y = obj.Layer * layerHeight;  // レイヤーごとにY座標を変える
            int height = layerHeight - 10; // オブジェクトの高さを設定

            // 選択されているかどうかに応じて色を変更
            Brush brush = obj.IsSelected ? Brushes.Red : Brushes.Blue;

            // オブジェクトの四角形を描画
            g.FillRectangle(Brushes.Blue, x, y, width, height);
            g.DrawRectangle(Pens.Black, x, y, width, height);

            // オブジェクト名（ファイル名）を描画
            g.DrawString(obj.FileName, SystemFonts.DefaultFont, Brushes.White, x + 5, y + 5);
        }

        // レイヤー間への線の描画
        private void DrawLayerLines(Graphics g, int numberOfLayers)
        {
            // レイヤーの高さ
            int layerHeight = 50;

            // 線の色とスタイルを設定
            Pen pen = new Pen(Color.Gray, 1); // グレーの線、太さ1ピクセル

            // 各レイヤー間に線を引く
            for (int i = 1; i < numberOfLayers; i++)
            {
                int y = i * layerHeight;
                g.DrawLine(pen, 0, y, this.Width, y); // この例では、Panelの幅全体に線を引きます
            }
        }

        //　PaintEventArgs を使用してカスタム描画をする
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;

            // タイムラインオブジェクトを描画
            var objects = _timeline.GetObjects();
            int scrollOffset = hScrollBar1.Value;
            foreach (var obj in objects)
            {
                DrawTimelineObject(g, obj, scrollOffset);
            }

            // レイヤー数を取得して線を描画
            if (objects.Any())  // リストが空でないことを確認
            {
                int numberOfLayers = objects.Max(o => o.Layer) + 1;
                DrawLayerLines(g, numberOfLayers);
            }
        }

        //　ファイルをドラッグしてフォームに入った際に、ドラッグ&ドロップで許可されるか判断する
        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        //　ドラッグ&ドロップした際に、ロードしてタイムラインに追加する
        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            var filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (var filePath in filePaths)
            {
                // 音声ファイルのロード
                _audioPlayer.Load(filePath);

                // 音声ファイルが正しくロードされているか確認
                if (_audioPlayer.TotalTime != TimeSpan.Zero)
                {
                    // タイムラインオブジェクトとして追加
                    var timelineObject = LoadAudioFile(filePath);
                    _timeline.AddObject(timelineObject);

                    // タイムラインの長さを音声ファイルの長さに合わせて更新
                    UpdateTrackBar(TimeSpan.Zero, _audioPlayer.TotalTime);

                    // タイムラインの終了時間を更新
                    UpdateTimelineEndTime();

                    // タイムラインの開始時間を更新
                    DisplayFirstObjectStartTime();

                    // タイムラインを再描画
                    panel1.Invalidate();
                }
                else
                {
                    MessageBox.Show($"音声ファイルの読み込みに失敗しました: {filePath}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        //　マウスクリックが行われた際に、オブジェクトを選択してドラッグ操作を開始する
        private void TimelinePanel_MouseDown(object sender, MouseEventArgs e)
        {
            // クリックされた位置にオブジェクトがあるか確認
            foreach (var obj in _timeline.GetObjects())
            {
                Rectangle objRect = new Rectangle(
                    (int)obj.StartTime.TotalMilliseconds,
                    obj.Layer * 20,
                    (int)obj.Duration.TotalMilliseconds,
                    20);

                if (objRect.Contains(e.Location))
                {
                    _selectedObject = obj;
                    _dragStartPoint = e.Location;
                    _isDragging = true;
                    break;
                }
            }
        }

        //　オブジェクトをドラッグして移動する際に、開始時間とレイヤーを更新してタイムラインを再描画する
        private void TimelinePanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && _selectedObject != null)
            {
                // マウスの移動量を計算
                int deltaX = e.X - _dragStartPoint.X;
                int deltaY = e.Y - _dragStartPoint.Y;

                // 1ピクセルあたりの時間単位を設定（例: 1ミリ秒あたり1ピクセル）
                double pixelsPerMillisecond = 1;

                // 再生開始時間を更新（移動量に基づいて開始時間を変更）
                TimeSpan newStartTime = _selectedObject.StartTime + TimeSpan.FromMilliseconds(deltaX * pixelsPerMillisecond);
                if (newStartTime < TimeSpan.Zero) newStartTime = TimeSpan.Zero;  // 開始時間を0未満にしない

                // 開始時間が負の値にならないように制限
                if (newStartTime < TimeSpan.Zero) newStartTime = TimeSpan.Zero;

                // オブジェクトの開始時間を更新
                _selectedObject.StartTime = newStartTime;

                // ドラッグ開始位置を更新
                _dragStartPoint = e.Location;

                // 新しいレイヤーを計算
                int newLayer = _selectedObject.Layer + deltaY / 20;
                if (newLayer < 0) newLayer = 0;  // レイヤーを0未満にしない

                // 更新した値を適用
                _selectedObject.StartTime = newStartTime;
                _selectedObject.Layer = newLayer;

                // 最も早い開始時間を表示
                DisplayFirstObjectStartTime();

                // タイムラインの終了時間を更新
                UpdateTimelineEndTime();

                // タイムラインを再描画
                panel1.Invalidate();
            }
        }

        //　ドラッグが終了した際に、オブジェクトが他のレイヤーに重なっているかを確認し、重なっている場合は空いているレイヤーに移動する
        private void TimelinePanel_MouseUp(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;

                // レイヤーの衝突を確認し、必要に応じてレイヤーを調整する
                int newLayer = _selectedObject.Layer;
                while (IsLayerOverlap(_selectedObject, newLayer))
                {
                    newLayer++;
                }
                _selectedObject.Layer = newLayer;

                _selectedObject = null;
                panel1.Invalidate();
            }
        }

        //　movingObject が targetLayer に移動した場合に、他のオブジェクトと重なっているかを判定
        private bool IsLayerOverlap(TimelineObject movingObject, int targetLayer)
        {
            foreach (var obj in _timeline.GetObjects())
            {
                if (obj != movingObject && obj.Layer == targetLayer && IsOverlapping(obj, movingObject))
                {
                    return true;
                }
            }
            return false;
        }

        // 2つのオブジェクトが重なっているか判定する
        private bool IsOverlapping(TimelineObject obj1, TimelineObject obj2)
        {
            // obj1の開始時間がobj2の終了時間より前で、かつobj1の終了時間がobj2の開始時間より後の場合、重なっている
            return obj1.StartTime < obj2.StartTime + obj2.Duration &&
                   obj1.StartTime + obj1.Duration > obj2.StartTime;
        }

        //　Addボタンを描画する
        private void Add_button1(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Audio Files|*.wav;*.mp3"
            };
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string filePath = openFileDialog.FileName;
                _audioPlayer.Load(filePath);

                // タイムラインオブジェクトとして追加
                var timelineObject = LoadAudioFile(filePath);
                _timeline.AddObject(timelineObject);

                // タイムラインの長さを音声ファイルの長さに合わせて更新
                UpdateTrackBar(TimeSpan.Zero, _audioPlayer.TotalTime);

                // 最も早い開始時間を表示
                DisplayFirstObjectStartTime();

                // タイムラインの終了時間を更新
                UpdateTimelineEndTime();

                // タイムラインを再描画
                panel1.Invalidate();
            }

        }

        //　オブジェクトを初期化する
        private TimelineObject LoadAudioFile(string fileName)
        {
            // 音声ファイルの長さを取得
            TimeSpan duration;
            using (var reader = new AudioFileReader(fileName))
            {
                duration = reader.TotalTime;
            }

            // ここでは、開始時間を0、レイヤーを0に設定
            return new TimelineObject(TimeSpan.Zero, duration, 0, fileName);
        }

        //　Cleanボタンを描画する
        private void Clean_button5(object sender, EventArgs e)
        {
            _audioPlayer.Clean();
            _timeline.GetObjects().Clear();
            panel1.Invalidate();
            DisplayFirstObjectStartTime();
            UpdateTimelineEndTime();
        }

        //　Playボタンを描画する
        private void Play_button2(object sender, EventArgs e)
        {
            if (!_isPlaying)
            {
                _isPlaying = true;
                _audioPlayer.Play();
                _playbackTimer.Start();
            }
        }

        //　Stopボタンを描画する
        private void Stop_button3(object sender, EventArgs e)
        {
            _isPlaying = false;
            _audioPlayer.Stop();
            _playbackTimer.Stop();
            _currentPlaybackTime = TimeSpan.Zero;
            UpdateTrackBar(_currentPlaybackTime, TimeSpan.FromSeconds(60));
        }

        //　Resetボタンを描画する
        private void Reset_button4(object sender, EventArgs e)
        {
            _audioPlayer.Reset();
            var currentTime = _audioPlayer.CurrentTime;
            label3.Text = $"Playback Time: {currentTime.ToString(@"hh\:mm\:ss")}";
        }

        //　Exportボタンを描画する
        private void Export_button6(object sender, EventArgs e)
        {

        }

        //　Playbackバーを描画する
        private void PlaybackBar_panel2(object sender, PaintEventArgs e)
        {

        }

        //　Playbackバーの位置を更新する
        private void UpdatePlaybackBar(TimeSpan currentTime, TimeSpan totalTime)
        {
            // タイムラインの全体の幅をピクセル単位で取得
            int timelineWidth = panel1.Width; // `panel1` はタイムラインの表示領域

            // 現在の再生位置に基づいてバーの幅を計算
            double percentage = currentTime.TotalMilliseconds / totalTime.TotalMilliseconds;
            int newWidth = (int)(timelineWidth * percentage);

            // 再生バーの位置を更新
            panel2.Width = newWidth;
        }

        //　現在の再生位置を表示する
        private void PlaybackTime_label3(object sender, EventArgs e)
        {

        }

        //　再生中の時間をリアルタイムで更新し、再生が終了したかチェックする
        private void PlaybackTimer_Tick(object sender, EventArgs e)
        {
            var currentTime = _audioPlayer.CurrentTime;

            // タイムラインが終了地点に達したかを確認
            if (currentTime >= _audioPlayer.TotalTime)
            {
                // 再生を停止
                StopPlayback();
            }
            else
            {
                // TrackBar とラベルを更新
                UpdateTrackBar(currentTime, _audioPlayer.TotalTime);
                label3.Text = $"Playback Time: {currentTime.ToString(@"hh\:mm\:ss")}";

                // 再生バーを更新
                UpdatePlaybackBar(currentTime, _audioPlayer.TotalTime);
            }
        }

        //　オーディオの再生を停止する際に、再生の終了処理とUIの更新をする
        private void StopPlayback()
        {
            // タイマーを停止
            _playbackTimer.Stop();

            // 音声再生を停止
            _audioPlayer.Stop();

            // TrackBar を終了地点にセット
            UpdateTrackBar(_audioPlayer.TotalTime, _audioPlayer.TotalTime);
        }

        //　PlaybackBar_panel2とPlaybackTime_label3を更新をする
        private void UpdateTrackBar(TimeSpan currentTime, TimeSpan totalTime)
        {
            trackBarTime.Minimum = 0;
            trackBarTime.Maximum = (int)totalTime.TotalMilliseconds;
            trackBarTime.Value = (int)currentTime.TotalMilliseconds;
            labelTime.Text = $"Time: {currentTime.ToString(@"hh\:mm\:ss")}";
        }

        //　最も早い再生開始時間を表示する
        private void StartTime_label2(object sender, EventArgs e)
        {

        }

        // タイムラインの開始時間を表示
        private void DisplayFirstObjectStartTime()
        {
            // タイムライン上の全オブジェクトを取得
            List<TimelineObject> timelineObjects = _timeline.GetObjects();

            // 最初のオブジェクトの開始時間を取得
            TimeSpan firstStartTime = GetFirstObjectStartTime(timelineObjects);

            // 開始時間をラベルに表示
            label2.Text = $"Start Time: {firstStartTime.ToString(@"hh\:mm\:ss")}";
        }

        // タイムラインの開始時間を取得
        private TimeSpan GetFirstObjectStartTime(List<TimelineObject> timelineObjects)
        {
            if (timelineObjects == null || timelineObjects.Count == 0)
            {
                return TimeSpan.Zero; // オブジェクトがない場合は0を返す
            }

            // 最初のオブジェクトの開始時間を取得
            TimeSpan firstStartTime = timelineObjects[0].StartTime;

            // 全オブジェクトの開始時間を確認し、最も早いものを選択
            foreach (var obj in timelineObjects)
            {
                if (obj.StartTime < firstStartTime)
                {
                    firstStartTime = obj.StartTime;
                }
            }

            return firstStartTime;
        }

        //　最も遅い再生終了時間を表示する
        private void EndTime_label1(object sender, EventArgs e)
        {

        }

        // タイムラインの終了時間を表示
        private void UpdateEndTimeLabel(TimeSpan endTime)
        {
            label1.Text = $"End Time: {endTime.ToString(@"hh\:mm\:ss")}";
        }

        // 終了時間を更新
        private void UpdateTimelineEndTime()
        {
            TimeSpan endTime = GetMaximumEndTime();
            UpdateEndTimeLabel(endTime);
        }

        // タイムラインオブジェクトの終了時間を比較
        private TimeSpan GetMaximumEndTime()
        {
            TimeSpan maxEndTime = TimeSpan.Zero;

            foreach (var obj in _timeline.GetObjects())
            {
                var objectEndTime = obj.StartTime + obj.Duration;
                if (objectEndTime > maxEndTime)
                {
                    maxEndTime = objectEndTime;
                }
            }

            return maxEndTime;
        }

        //　timelineの横スクロールバーを描画する
        private void Timeline_hScrollBar1(object sender, ScrollEventArgs e)
        {
            // スクロールバーの値がMinimumとMaximumの範囲内であるかを確認
            if (e.NewValue >= hScrollBar1.Minimum && e.NewValue <= hScrollBar1.Maximum)
            {
                hScrollBar1.Value = e.NewValue;
                // スクロール位置に基づいてタイムラインを再描画
                panel1.Invalidate(); // これでパネルの再描画がトリガーされます
            }
        }

        // フォームの初期化時にスクロールバーの設定
        private void hScrollBar1_Load(object sender, EventArgs e)
        {
            // スクロールバーの設定
            hScrollBar1.Minimum = 0;
            hScrollBar1.Maximum = CalculateMaxScroll(); // タイムラインの最大幅に応じて設定
            hScrollBar1.LargeChange = ClientSize.Width; // 表示領域の幅
            hScrollBar1.SmallChange = 10; // スクロール時の小さな変化量
            hScrollBar1.Value = 0; // 初期値
        }

        // タイムラインの最大スクロール幅を計算するメソッド
        private int CalculateMaxScroll()
        {
            // タイムライン上の最も右側のオブジェクトの位置を計算
            int maxRight = 0;
            foreach (var obj in timelineObjects)
            {
                int right = (int)(obj.StartTime.TotalMilliseconds * pixelsPerMillisecond) +
                            (int)(obj.Duration.TotalMilliseconds * pixelsPerMillisecond);
                if (right > maxRight)
                {
                    maxRight = right;
                }
            }
            return maxRight - ClientSize.Width; // スクロールの最大幅
        }

        //　タイムライン上のオーディオオブジェクトを管理する
        public class Timeline
        {
            private List<TimelineObject> _objects = new List<TimelineObject>();

            //　新しいオブジェクトをタイムラインに追加する
            public void AddObject(TimelineObject newObject)
            {
                int targetLayer = 0;
                bool overlapFound;

                do
                {
                    overlapFound = false;

                    // 同じレイヤー内のオブジェクトとの衝突をチェック
                    foreach (var existingObject in _objects)
                    {
                        if (existingObject.Layer == targetLayer && IsOverlapping(existingObject, newObject))
                        {
                            overlapFound = true;
                            targetLayer++;  // レイヤーを1つ上に移動
                            break;
                        }
                    }
                } while (overlapFound);

                // 衝突のないレイヤーが見つかったので、そのレイヤーに配置
                newObject.Layer = targetLayer;
                _objects.Add(newObject);
            }

            // オブジェクトが重なっているかどうかを判定する
            private bool IsOverlapping(TimelineObject obj1, TimelineObject obj2)
            {
                return obj1.StartTime < obj2.StartTime + obj2.Duration &&
                       obj2.StartTime < obj1.StartTime + obj1.Duration;
            }

            //　指定したオブジェクトをタイムラインから削除する
            public void RemoveObject(TimelineObject obj)
            {
                _objects.Remove(obj);
            }

            //　タイムライン上のすべてのオブジェクトのリストを返す
            public List<TimelineObject> GetObjects()
            {
                return _objects;
            }
        }

        // オブジェクトのプロパティを保持する
        public class TimelineObject
        {
            public TimeSpan StartTime { get; set; }
            public TimeSpan Duration { get; set; }
            public TimeSpan EndTime => StartTime + Duration;
            public int Layer { get; set; }
            public string FilePath { get; set; }
            public WaveStream WaveStream { get; set; }
            public bool IsSelected { get; set; } // 選択状態

            public string FileName => Path.GetFileName(FilePath);

            //　初期化
            public TimelineObject(TimeSpan startTime, TimeSpan duration, int layer, string filePath)
            {
                StartTime = startTime;
                Duration = duration;
                Layer = layer;
                FilePath = filePath;
                IsSelected = false; // 初期状態では選択されていない
            }
        }

        // 再生関連の機能を提供
        public class AudioPlayer
        {
            private List<IWavePlayer> _wavePlayers;
            private List<WaveStream> _waveStreams;

            // 初期化
            public AudioPlayer()
            {
                _wavePlayers = new List<IWavePlayer>();
                _waveStreams = new List<WaveStream>();
            }

            //　指定されたファイルパスからオーディオファイルを読み込む
            public void Load(string filePath)
            {
                var wavePlayer = new WaveOutEvent();
                var waveStream = new AudioFileReader(filePath);

                wavePlayer.Init(waveStream);
                _wavePlayers.Add(wavePlayer);
                _waveStreams.Add(waveStream);

                // Durationは、waveStreamから取得する
                TimeSpan duration = waveStream.TotalTime;

                // TimelineObjectを作成して情報を格納
                var TimelineObject = new TimelineObject(
                    startTime: CurrentTime,
                    duration: duration,
                    layer: 0,
                    filePath: filePath
                )
                {
                    WaveStream = waveStream,
                    IsSelected = false // 初期状態では選択されていないものとする
                };
            }

            // すべてのオーディオプレイヤーで再生する
            public void Play()
            {
                foreach (var player in _wavePlayers)
                {
                    player.Play();
                }
            }

            //　すべてのオーディオプレイヤーで再生を停止する
            public void Stop()
            {
                foreach (var player in _wavePlayers)
                {
                    player.Stop();
                }
            }

            //　すべてのオーディオストリームの再生位置を先頭に戻す
            public void Reset()
            {
                foreach (var stream in _waveStreams)
                {
                    stream.Position = 0; // 再生位置を先頭に戻す
                }
            }

            // すべてのオブジェクトを削除する
            public void Clean()
            {
                // リソースを解放してからリストをクリアする
                foreach (var player in _wavePlayers)
                {
                    player.Dispose();
                }
                foreach (var stream in _waveStreams)
                {
                    stream.Dispose();
                }

                _wavePlayers.Clear();
                _waveStreams.Clear();
            }

            // 現在の再生位置を取得する
            public TimeSpan CurrentTime
            {
                get
                {
                    if (_waveStreams.Count > 0)
                        return _waveStreams[0].CurrentTime;
                    return TimeSpan.Zero;
                }
            }

            // オーディオファイルの総再生時間を取得する
            public TimeSpan TotalTime
            {
                get
                {
                    if (_waveStreams.Count > 0)
                        return _waveStreams[0].TotalTime;
                    return TimeSpan.Zero;
                }
            }
        }
    }
}
