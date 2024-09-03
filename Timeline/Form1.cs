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
        }

        private void PlaybackTimer_Tick(object sender, EventArgs e)
        {
            // 現在の再生位置を取得
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
            int x = (int)obj.StartTime.TotalMilliseconds;  // ミリ秒をピクセルに変換
            int width = (int)obj.Duration.TotalMilliseconds;  // ミリ秒をピクセルに変換
            int y = obj.Layer * 20;  // レイヤーごとにY座標を変える
            int height = 20;

            g.FillRectangle(Brushes.Blue, x, y, width, height);
            g.DrawRectangle(Pens.Black, x, y, width, height);
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

                // 音声ファイルをタイムラインオブジェクトとして追加
                var timelineObject = new TimelineObject(TimeSpan.Zero, _audioPlayer.TotalTime, 0);
                _timeline.AddObject(timelineObject);

                // タイムラインの長さを音声ファイルの長さに合わせて更新
                UpdateTrackBar(TimeSpan.Zero, _audioPlayer.TotalTime);

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

        private void TrackBar(object sender, EventArgs e)
        {
            int trackBarValue = trackBarTime.Value;
            TimeSpan newPlaybackTime = TimeSpan.FromMilliseconds(trackBarValue);

            // 再生位置をタイムラインに設定するメソッドを呼び出す
            // _timelinePlayer.SetPlaybackPosition(newPlaybackTime); // 実際のタイムラインプレーヤーに依存

            // UI に現在の時間を表示
            labelTime.Text = $"Time: {newPlaybackTime.ToString(@"hh\:mm\:ss")}";

            // タイムラインプレーヤーに再生位置を設定する
            _audioPlayer.Stop(); // 既に再生中の場合は一旦停止
            _audioPlayer.Load(_audioPlayer.TotalTime.ToString()); // 再読み込みして位置を移動
            _audioPlayer.Play();
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

        private void trackBar1_Scroll(object sender, EventArgs e)
        {

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

        private void Button_Pause(object sender, EventArgs e)
        {
            if (_isPlaying)
            {
                _isPlaying = false;
                _playbackTimer.Stop();
            }
        }
    }

    public class Timeline
    {
        private List<TimelineObject> _objects = new List<TimelineObject>();

        public void AddObject(TimelineObject obj)
        {
            _objects.Add(obj);
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
        private IWavePlayer _wavePlayer;
        private WaveStream _waveStream;

        public AudioPlayer()
        {
            _wavePlayer = new WaveOutEvent();
        }

        public void Load(string filePath)
        {
            _waveStream = new AudioFileReader(filePath);
            _wavePlayer.Init(_waveStream);
        }

        public void Play()
        {
            _wavePlayer.Play();
        }

        public void Pause()
        {
            _wavePlayer.Pause();
        }

        public void Stop()
        {
            _wavePlayer.Stop();
            _waveStream.Position = 0; // 再生位置を先頭に戻す
        }

        public TimeSpan CurrentTime => _waveStream.CurrentTime;
        public TimeSpan TotalTime => _waveStream.TotalTime;
    }
}
