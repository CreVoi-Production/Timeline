using System;
using System.Drawing;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Collections.Generic;
using System.IO;
using NAudio.Wave;

namespace Timeline
{
    public partial class Form1 : Form
    {
        private Timeline _timeline;
        private System.Windows.Forms.Timer _playbackTimer;
        private TimeSpan _currentPlaybackTime;
        private bool _isPlaying;
        private AudioPlayer _audioPlayer;
        private TimelineObject _selectedObject;
        private Point _dragStartPoint;
        private bool _isDragging;

        public Form1()
        {
            InitializeComponent();

            _timeline = new Timeline();
            _playbackTimer = new System.Windows.Forms.Timer();
            _playbackTimer.Interval = 100; // ミリ秒単位、ここでは100msごとに更新
            _playbackTimer.Tick += PlaybackTimer_Tick;

            // TrackBar の初期設定
            UpdateTrackBar(TimeSpan.Zero, TimeSpan.FromSeconds(10)); // 10秒のタイムライン

            _audioPlayer = new AudioPlayer();

            // ドラッグ＆ドロップの設定
            this.AllowDrop = true;
            this.DragEnter += Form1_DragEnter;
            this.DragDrop += Form1_DragDrop;

            //オブジェクトのドラッグ移動
            panel1.MouseDown += TimelinePanel_MouseDown;
            panel1.MouseMove += TimelinePanel_MouseMove;
            panel1.MouseUp += TimelinePanel_MouseUp;
        }

        // タイムラインの開始時間を表示
        private void DisplayFirstObjectStartTime()
        {
            // タイムライン上の全オブジェクトを取得
            List<TimelineObject> timelineObjects = _timeline.GetObjects(); // このメソッドはタイムラインのオブジェクトリストを取得するものと仮定

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

        //　ドラッグ＆ドロップの実装
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

        //　ドラッグ＆ドロップの実装
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

        //オブジェクトのドラッグ移動
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

        //オブジェクトのドラッグ移動
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

        //オブジェクトのドラッグ移動
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

        // オブジェクトの重なり判定
        private bool IsOverlapping(TimelineObject obj1, TimelineObject obj2)
        {
            // obj1の開始時間がobj2の終了時間より前で、かつobj1の終了時間がobj2の開始時間より後の場合、重なっている
            return obj1.StartTime < obj2.StartTime + obj2.Duration &&
                   obj1.StartTime + obj1.Duration > obj2.StartTime;
        }

        //再生バーの位置を更新
        private void UpdatePlaybackBar(TimeSpan currentTime, TimeSpan totalTime)
        {
            // タイムラインの全体の幅をピクセル単位で取得
            int timelineWidth = panel1.Width; // `panel1` はタイムラインの表示領域

            // 現在の再生位置に基づいてバーの幅を計算
            double percentage = currentTime.TotalMilliseconds / totalTime.TotalMilliseconds;
            int newWidth = (int)(timelineWidth * percentage);

            // 再生バーの位置を更新
            panelPlaybackBar.Width = newWidth;
        }

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
                labelPlaybackTime.Text = $"Playback Time: {currentTime.ToString(@"hh\:mm\:ss")}";

                // 再生バーを更新
                UpdatePlaybackBar(currentTime, _audioPlayer.TotalTime);
            }
        }

        private void StopPlayback()
        {
            // タイマーを停止
            _playbackTimer.Stop();

            // 音声再生を停止
            _audioPlayer.Stop();

            // TrackBar を終了地点にセット
            UpdateTrackBar(_audioPlayer.TotalTime, _audioPlayer.TotalTime);
        }

        private void TimelinePanel_Paint(object sender, PaintEventArgs e)
        {
            // タイムラインオブジェクトを描画する処理
            foreach (var obj in _timeline.GetObjects())
            {
                DrawTimelineObject(e.Graphics, obj);
            }
        }

        private void DrawTimelineObject(Graphics g, TimelineObject obj)
        {
            // オブジェクトの位置とサイズを計算して描画
            int pixelsPerMillisecond = 1; // 1ミリ秒あたりのピクセル数（時間軸のスケール）
            int layerHeight = 50; // 各レイヤーの高さ

            int x = (int)(obj.StartTime.TotalMilliseconds * pixelsPerMillisecond);  // ミリ秒をピクセルに変換
            int width = (int)(obj.Duration.TotalMilliseconds * pixelsPerMillisecond);  // ミリ秒をピクセルに変換
            int y = obj.Layer * layerHeight;  // レイヤーごとにY座標を変える
            int height = layerHeight - 10; // オブジェクトの高さを設定

            // オブジェクトの四角形を描画
            g.FillRectangle(Brushes.Blue, x, y, width, height);
            g.DrawRectangle(Pens.Black, x, y, width, height);

            // オブジェクト名や他の情報を描画したい場合
            g.DrawString("Object", SystemFonts.DefaultFont, Brushes.White, x + 5, y + 5);
        }

        private void Button_AddObject(object sender, EventArgs e)
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

        private TimelineObject LoadAudioFile(string filePath)
        {
            // 音声ファイルの長さを取得
            TimeSpan duration;
            using (var reader = new AudioFileReader(filePath))
            {
                duration = reader.TotalTime;
            }

            // ここでは、開始時間を0、レイヤーを0に設定
            return new TimelineObject(TimeSpan.Zero, duration, 0);
        }

        private void UpdateTrackBar(TimeSpan currentTime, TimeSpan totalTime)
        {
            trackBarTime.Minimum = 0;
            trackBarTime.Maximum = (int)totalTime.TotalMilliseconds;
            trackBarTime.Value = (int)currentTime.TotalMilliseconds;
            labelTime.Text = $"Time: {currentTime.ToString(@"hh\:mm\:ss")}";
        }

        private void Button_Play(object sender, EventArgs e)
        {
            if (!_isPlaying)
            {
                _isPlaying = true;
                _audioPlayer.Play();
                _playbackTimer.Start();
            }
        }

        private void Button_Stop(object sender, EventArgs e)
        {
            _isPlaying = false;
            _audioPlayer.Stop();
            _playbackTimer.Stop();
            _currentPlaybackTime = TimeSpan.Zero;
            UpdateTrackBar(_currentPlaybackTime, TimeSpan.FromSeconds(60));
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void panel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void Label_EndTime(object sender, EventArgs e)
        {

        }

        private void Label_PlaybackTime(object sender, EventArgs e)
        {

        }

        private void Label_StartTime(object sender, EventArgs e)
        {

        }

        private void Button_Reset(object sender, EventArgs e)
        {
            _audioPlayer.Reset();
            var currentTime = _audioPlayer.CurrentTime;
            labelPlaybackTime.Text = $"Playback Time: {currentTime.ToString(@"hh\:mm\:ss")}";
        }
    }

    public class Timeline
    {
        private List<TimelineObject> _objects = new List<TimelineObject>();

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

        // オブジェクトが重なっているかどうかを判定するメソッド
        private bool IsOverlapping(TimelineObject obj1, TimelineObject obj2)
        {
            return obj1.StartTime < obj2.StartTime + obj2.Duration &&
                   obj2.StartTime < obj1.StartTime + obj1.Duration;
        }

        public void RemoveObject(TimelineObject obj)
        {
            _objects.Remove(obj);
        }

        public List<TimelineObject> GetObjects()
        {
            return _objects;
        }
    }

    public class TimelineObject
    {
        public TimeSpan StartTime { get; set; }
        public TimeSpan Duration { get; set; }
        public int Layer { get; set; }

        public TimelineObject(TimeSpan startTime, TimeSpan duration, int layer)
        {
            StartTime = startTime;
            Duration = duration;
            Layer = layer;
        }
    }

    public class AudioPlayer
    {
        private List<IWavePlayer> _wavePlayers;
        private List<WaveStream> _waveStreams;

        public AudioPlayer()
        {
            _wavePlayers = new List<IWavePlayer>();
            _waveStreams = new List<WaveStream>();
        }

        public void Load(string filePath)
        {
            var wavePlayer = new WaveOutEvent();
            var waveStream = new AudioFileReader(filePath);

            wavePlayer.Init(waveStream);
            _wavePlayers.Add(wavePlayer);
            _waveStreams.Add(waveStream);
        }

        public void Play()
        {
            foreach (var player in _wavePlayers)
            {
                player.Play();
            }
        }

        public void Stop()
        {
            foreach (var player in _wavePlayers)
            {
                player.Stop();
            }
        }

        public void Reset()
        {
            foreach (var stream in _waveStreams)
            {
                stream.Position = 0; // 再生位置を先頭に戻す
            }
        }

        public TimeSpan CurrentTime
        {
            get
            {
                if (_waveStreams.Count > 0)
                    return _waveStreams[0].CurrentTime;
                return TimeSpan.Zero;
            }
        }

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
