using System;
using System.Drawing;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NAudio.Lame;
using System.Text;
using static Timeline.Form1;
using System.Numerics;
using System.Diagnostics;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolBar;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using NAudio.CoreAudioApi;
using System.Media;
using ExportProgressDialog;

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
        private int layerHeight = 50; // 各レイヤーの高さ
        private double pixelsPerMillisecond => (double)panel1.ClientSize.Width / _timeline.TotalDuration.TotalMilliseconds; // 1ミリ秒あたりのピクセル数（時間軸のスケール）　
        private WaveInEvent waveIn;
        private WaveFileWriter waveFileWriter;
        private bool isRecording = false;
        private SerialPort serialPort;
        private string sendfilePath;

        private static readonly HttpClient client = new HttpClient();   // VOICEVOX クライアント
        private const string VOICEVOXurl = "http://127.0.0.1:50021";    // VOICEVOX サーバーアドレス

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

            serialPort = new SerialPort();

            // 利用可能なCOMポートを取得
            string[] availablePorts = SerialPort.GetPortNames();

            // 利用可能なポートを表示（デバッグや選択用）
            //foreach (string port in availablePorts)
            //{
            //    MessageBox.Show("Available Port: " + port);
            //}

            // 使用するポートをセット
            if (availablePorts != null && availablePorts.Length > 0)
            {
                // シリアルポートの初期設定
                serialPort = new SerialPort
                {
                    BaudRate = 115200,         // ボーレートを115200bpsに設定
                    Parity = Parity.None,       // パリティをなしに設定
                    DataBits = 8,               // データビットを8に設定
                    StopBits = StopBits.One,    // ストップビットを1に設定
                    PortName = availablePorts[0]           // COMポート名を設定 (実際の環境に合わせて変更)
                };
            }
            else
            {
                MessageBox.Show("利用可能なシリアルポートがありません。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            try
            {
                serialPort.Open(); // シリアルポートを開く
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show("他のプロセスがポートを使用している可能性があります: " + ex.Message);
            }
            catch (FileNotFoundException ex)
            {
                MessageBox.Show("指定したポートが存在しません: " + ex.Message);
            }
            catch (IOException ex)
            {
                MessageBox.Show("シリアルポートの入出力エラーが発生しました: " + ex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show("予期しないエラーが発生しました: " + ex.Message);
            }
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

            // 描画座標をオブジェクトに設定
            obj.DrawingPosition = new Point(x, y);

            // 選択されているかどうかに応じて色を変更
            Brush brush = obj.IsSelected ? Brushes.Red : Brushes.Blue;

            // オブジェクトの四角形を描画
            g.FillRectangle(brush, x, y, width, height);
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

        //　時間をピクセル位置に変換する
        private int TimeToPixel(TimeSpan time)
        {
            return (int)(time.TotalMilliseconds * pixelsPerMillisecond);
        }

        //　ピクセル位置を時間に変換する
        private TimeSpan PixelToTime(int pixel)
        {
            return TimeSpan.FromMilliseconds(pixel / pixelsPerMillisecond);
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

            // 時間ラベルを描画する
            DrawTimeLabels(g);
        }

        // タイムラインの時間ラベルを描画する　
        private void DrawTimeLabels(Graphics graphics)
        {
            if (_timeline.TotalDuration == TimeSpan.Zero)
                return;

            // タイムラインの全体にわたる時間のラベルを描画
            double PixelsPerMillisecond = pixelsPerMillisecond;
            TimeSpan duration = _timeline.TotalDuration;
            int numberOfLabels = (int)(duration.TotalMinutes) + 1; // 分単位でラベルを設定

            Font font = new Font("Arial", 8);
            Brush brush = Brushes.Black;

            for (int i = 0; i <= numberOfLabels; i++)
            {
                TimeSpan labelTime = TimeSpan.FromMinutes(i * 1); // 1分単位のラベル
                int xPosition = TimeToPixel(labelTime);

                // ラベルテキストの描画
                string timeLabel = labelTime.ToString(@"hh\:mm\:ss");
                graphics.DrawString(timeLabel, font, brush, xPosition, 0);
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
                TimelineObject timelineObject = _audioPlayer.Load(filePath);

                // 音声ファイルが正しくロードされているか確認
                if (_audioPlayer.TotalTime != TimeSpan.Zero)
                {
                    // タイムラインオブジェクトとして追加
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
            if (e.Button == MouseButtons.Left) // 左クリックでドラッグを開始
            {
                // 全てのオブジェクトの選択状態をクリア
                foreach (var obj in _timeline.GetObjects())
                {
                    obj.IsSelected = false; // 選択解除
                }

                // クリックされた位置にオブジェクトがあるか確認
                foreach (var obj in _timeline.GetObjects())
                {
                    // `DrawingPosition` を利用してオブジェクトの矩形を計算
                    Rectangle objRect = new Rectangle(
                        obj.DrawingPosition.X,
                        obj.DrawingPosition.Y,
                        (int)(obj.Duration.TotalMilliseconds * pixelsPerMillisecond),
                        layerHeight - 10);

                    if (objRect.Contains(e.Location))
                    {
                        obj.IsSelected = true;
                        _selectedObject = obj;
                        _dragStartPoint = e.Location;
                        _isDragging = true;
                        break;
                    }
                }

                panel1.Invalidate();
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

                // スケーリングファクターを設定
                double scaleFactor = 1;

                // 1ピクセルあたりの時間単位を設定 (1ミリ秒あたり1ピクセル）
                double pixelsPerMillisecond = 1;

                // スケーリングファクターを適用
                double scaledDeltaX = deltaX * scaleFactor;
                double scaledDeltaY = deltaY * scaleFactor;

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

                // 描画座標を再計算して更新
                _selectedObject.DrawingPosition = new Point(
                    (int)(newStartTime.TotalMilliseconds * pixelsPerMillisecond) - hScrollBar1.Value,
                    _selectedObject.Layer * layerHeight
                );

                // 最も早い開始時間を表示
                DisplayFirstObjectStartTime();

                // タイムラインの終了時間を更新
                UpdateTimelineEndTime();

                // 音声プレーヤーの開始位置を更新
                UpdateAudioPlayerStartTime(_selectedObject);

                // タイムラインを再描画
                panel1.Invalidate();
            }
        }

        //　TimelineObjectのStartTimeを更新し、waveStreamとwavePlayerを対応するStartTimeに基づいて更新する
        private void UpdateAudioPlayerStartTime(TimelineObject obj)
        {
            // 1ミリ秒あたりのバイト数を設定（実際の実装に応じて調整）
            const double BytesPerMillisecond = 44.1 * 2 * 2 / 1000; // 44.1kHz ステレオ 16ビット

            // StartTime をミリ秒に変換
            double startMilliseconds = obj.StartTime.TotalMilliseconds;

            // waveStream の再生位置を設定
            if (_audioPlayer.waveStream != null)
            {
                long newPosition = (long)(startMilliseconds * BytesPerMillisecond);
                _audioPlayer.waveStream.Position = newPosition;
            }

            // wavePlayer の再生位置を設定
            if (_audioPlayer != null && _audioPlayer.wavePlayer != null)
            {
                _audioPlayer.wavePlayer.Stop();
                _audioPlayer.wavePlayer.Play();
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

                try
                {
                    // ファイルをタイムラインに追加
                    AddToTimeline(filePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"ファイルの読み込みに失敗しました: {ex.Message}");
                }
            }
        }

        // ファイルをタイムラインに追加する共通メソッド
        private void AddToTimeline(string filePath)
        {
            // ファイルパスのチェック
            if (string.IsNullOrEmpty(filePath))
            {
                MessageBox.Show("無効なファイルパスです。");
                return;
            }

            // タイムラインオブジェクトの作成と追加
            TimelineObject timelineObject = _audioPlayer.Load(filePath);
            _timeline.AddObject(timelineObject);

            // タイムラインの長さを音声ファイルの長さに合わせて更新
            UpdateTrackBar(TimeSpan.Zero, GetMaximumEndTime());

            // 最も早い開始時間を表示
            DisplayFirstObjectStartTime();

            // タイムラインの終了時間を更新
            UpdateTimelineEndTime();

            // タイムラインを再描画
            panel1.Invalidate();
        }


        //　Cleanボタンを描画する
        private void Clean_button5(object sender, EventArgs e)
        {
            _audioPlayer.Reset();
            _isPlaying = false;
            _audioPlayer.Stop();
            _playbackTimer.Stop();
            var currentTime = _audioPlayer.CurrentTime;
            label3.Text = $"Playback Time: {currentTime.ToString(@"hh\:mm\:ss")}";

            _audioPlayer.Clean();
            _timeline.GetObjects().Clear();
            panel1.Invalidate();
            DisplayFirstObjectStartTime();
            UpdateTimelineEndTime();
        }

        //　Deleteボタンを描画する
        private void Delete_button7(object sender, EventArgs e)
        {
            // 選択されているオブジェクトを探す
            var selectedObjects = _timeline.GetSelectedObjects();

            if (selectedObjects.Count > 0)
            {
                // Timelineリストから選択されたオブジェクトを削除
                foreach (var obj in selectedObjects)
                {
                    _timeline.RemoveObject(obj);
                }

                // WaveStreamリストから該当するオブジェクトを削除
                _audioPlayer.Delete(selectedObjects);

                // 選択状態を解除
                _selectedObject = null;

                // タイムラインを再描画
                panel1.Invalidate();
            }
            else
            {
                // 選択されているオブジェクトがない場合
                MessageBox.Show("削除するオブジェクトが選択されていません。");
            }
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
            _isPlaying = false;
            _audioPlayer.Stop();
            _playbackTimer.Stop();
            var currentTime = _audioPlayer.CurrentTime;
            label3.Text = $"Playback Time: {currentTime.ToString(@"hh\:mm\:ss")}";
        }

        //　Exportボタンを描画する(録音式)
        private async void Export_button6(object sender, EventArgs e)
        {
            //using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            //{
            //    saveFileDialog.Filter = "WAVファイル (*.wav)|*.wav"; // フィルタを設定
            //    saveFileDialog.Title = "WAVファイルを保存";
            //    saveFileDialog.DefaultExt = "wav"; // デフォルトの拡張子

            //    if (saveFileDialog.ShowDialog() == DialogResult.OK)
            //    {
            //        string outputFilePath = saveFileDialog.FileName;
            //        sendfilePath = saveFileDialog.FileName;

            //        // 録音と再生の開始
            //        StartRecording(outputFilePath);
            //        _audioPlayer.Play();
            //    }
            //}

            // 保存ダイアログの表示
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "WAVファイル (*.wav)|*.wav";
                saveFileDialog.Title = "保存先を指定";
                saveFileDialog.FileName = "timeline_mix_output.wav";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string outputFilePath = saveFileDialog.FileName;

                    // WAVファイルの書き出し処理を実行
                    await ExportTimelineMixToWav(outputFilePath);
                }
            }
        }

        // タイムライン上のすべてのWaveOffsetStreamをミックスしてWAVに書き出す
        private async Task ExportTimelineMixToWav(string outputFilePath)
        {
            // タイムライン上のオフセットストリームをミックス
            var offsetStreams = _audioPlayer._waveOffsetStreams
                                            .Where(stream => stream != null)
                                            .Select(stream => stream.ToSampleProvider())
                                            .ToList();

            if (!offsetStreams.Any())
            {
                MessageBox.Show("ミックス対象のオフセットストリームがありません。");
                return;
            }

            // MixingSampleProviderの初期化
            var mixingSampleProvider = new MixingSampleProvider(offsetStreams);

            // プログレスダイアログの表示
            var progressDialog = new ExportProgressDialog.Form2();
            progressDialog.Show(); // プログレスダイアログを表示

            // WAVファイルに書き出し
            using (var waveFileWriter = new WaveFileWriter(outputFilePath, mixingSampleProvider.WaveFormat))
            {
                float[] buffer = new float[1024];
                int totalSamples = mixingSampleProvider.WaveFormat.SampleRate * 60; // 仮に1分間のサンプル数を想定（適宜調整）
                int processedSamples = 0;

                await Task.Run(() =>
                {
                    int samplesRead;
                    while ((samplesRead = mixingSampleProvider.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        waveFileWriter.WriteSamples(buffer, 0, samplesRead);
                        processedSamples += samplesRead;

                        // 進捗を計算
                        var progress = (double)processedSamples / totalSamples * 100;

                        // プログレスダイアログを更新
                        for (progress = 0; progress <= 100; progress += 10)
                        {
                            // 進捗を更新
                            progressDialog.UpdateProgress(progress, $"進行状況: {progress}%");
                        }
                    }
                });
            }

            // 書き出し後、MixingSampleProviderを揮発させる
            mixingSampleProvider = null;

            // プログレスダイアログを閉じる
            progressDialog.Invoke((MethodInvoker)delegate
            {
                progressDialog.Close();
            });

            MessageBox.Show("WAVファイルの書き出しが完了しました。");
        }

        // 録音を開始する
        private void StartRecording(string filePath)
        {
            waveIn = new WaveInEvent();
            waveIn.DeviceNumber = 0; // デフォルトのマイク
            waveIn.WaveFormat = new WaveFormat(8000, 8, 1); // 8.0kHz、8bit、モノラル

            waveIn.DataAvailable += OnDataAvailable;
            waveIn.RecordingStopped += OnRecordingStopped;

            waveFileWriter = new WaveFileWriter(filePath, waveIn.WaveFormat);

            _audioPlayer.Reset();
            waveIn.StartRecording();
            _playbackTimer.Start();
            isRecording = true;

            // 再生タイマーの設定
            _playbackTimer = new System.Windows.Forms.Timer();
            _playbackTimer.Interval = 100; // 100ミリ秒ごとに更新
            _playbackTimer.Tick += OnPlaybackTick;
            _playbackTimer.Start();
        }

        // 再生タイマーのTickイベント
        private void OnPlaybackTick(object sender, EventArgs e)
        {
            var currentTime = _audioPlayer.CurrentTime;
            var maxEndTime = GetMaximumEndTime();

            // maxEndTimeに達したら停止
            if (currentTime >= maxEndTime)
            {
                StopRecording();
                StopPlayback();
            }
        }

        // 録音を停止する
        private void StopRecording()
        {
            if (isRecording)
            {
                waveIn.StopRecording();
                isRecording = false;
            }
        }

        // 録音データをファイルに書き込む
        private void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            if (waveFileWriter != null)
            {
                waveFileWriter.Write(e.Buffer, 0, e.BytesRecorded);
            }
        }

        // 録音停止後の処理
        private void OnRecordingStopped(object sender, StoppedEventArgs e)
        {
            waveFileWriter?.Dispose();
            waveFileWriter = null;
        }

        //　Sendボタンを描画する
        private async void Send_button10(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(sendfilePath))
            {
                try
                {
                    // WAVファイルのバイナリを読み込む
                    byte[] fileBytes = await Task.Run(() => File.ReadAllBytes(sendfilePath));

                    // 読み込んだバイナリデータの最初の512バイトを表示
                    // int displayLength = Math.Min(fileBytes.Length, 512); // 最初の512バイトだけ取得
                    // string firstHexString = BitConverter.ToString(fileBytes.Take(displayLength).ToArray());

                    // 読み込んだバイナリデータの最後の512バイトを表示
                    // byte[] lastBytes = fileBytes.Skip(Math.Max(0, fileBytes.Length - 512)).ToArray();
                    // string lastHexString = BitConverter.ToString(lastBytes);

                    // メッセージボックスに表示
                    // MessageBox.Show($"最初の512バイトの16進数形式:\n{firstHexString}");
                    // MessageBox.Show($"最後の512バイトの16進数形式:\n{lastHexString}");

                    // バイナリの先頭に指定したバイトを追加するための配列を作成
                    byte[] modifiedBytes = new byte[fileBytes.Length + 11]; // 5 + 2（0x00 と 0xFF）
                    modifiedBytes[0] = 0x53; // 'S'
                    modifiedBytes[1] = 0x54; // 'T'
                    modifiedBytes[2] = 0x41; // 'A'
                    modifiedBytes[3] = 0x52; // 'R'
                    modifiedBytes[4] = 0x54; // 'T'

                    // WAVファイルのバイナリを modifiedBytes の 5 バイト目から追加
                    Array.Copy(fileBytes, 0, modifiedBytes, 5, fileBytes.Length);

                    // 末尾に追加のバイトを設定
                    int startIndex = 5 + fileBytes.Length; // 追加バイトの開始インデックス
                    modifiedBytes[startIndex] = 0x45; // 'E'
                    modifiedBytes[startIndex + 1] = 0x4E; // 'N'
                    modifiedBytes[startIndex + 2] = 0x44; // 'D'
                    modifiedBytes[startIndex + 3] = 0x49; // 'I'
                    modifiedBytes[startIndex + 4] = 0x4E; // 'N'
                    modifiedBytes[startIndex + 5] = 0x47; // 'G'

                    // 変更後のバイナリデータを最初の512バイトを表示
                    // int displayLength2 = Math.Min(modifiedBytes.Length, 512); // 最初の512バイトだけ表示
                    // string modifiedHexString = BitConverter.ToString(modifiedBytes.Take(displayLength2).ToArray());

                    // 変更後バイナリデータの最後の512バイトを表示
                    // byte[] lastBytes2 = modifiedBytes.Skip(Math.Max(0, modifiedBytes.Length - 512)).ToArray();
                    // string lastHexString2 = BitConverter.ToString(lastBytes2);

                    // メッセージボックスに表示
                    // MessageBox.Show($"変更後の512ファイルの16進数形式:\n{modifiedHexString}");
                    // MessageBox.Show($"変更後の512バイトの16進数形式:\n{lastHexString2}");

                    // ファイル保存ダイアログを表示
                    using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                    {
                        saveFileDialog.Filter = "Text files (*.txt)|*.txt"; // テキストファイルのみ
                        saveFileDialog.Title = "バイナリデータの保存先を指定してください";
                        saveFileDialog.FileName = "binary_output.txt"; // デフォルトのファイル名

                        // ユーザーが保存先を指定した場合のみ処理を進める
                        if (saveFileDialog.ShowDialog() == DialogResult.OK)
                        {
                            // バイナリデータをテキストファイルに保存
                            string hexData = BitConverter.ToString(modifiedBytes).Replace("-", " "); // バイナリデータを16進数形式に変換
                            await File.WriteAllTextAsync(saveFileDialog.FileName, hexData); // テキストファイルに保存
                            MessageBox.Show($"バイナリデータを {saveFileDialog.FileName} に保存しました。");
                        }
                        else
                        {
                            MessageBox.Show("ファイル保存がキャンセルされました。");
                        }
                    }

                    // CR通信で送信
                    if (serialPort.IsOpen && modifiedBytes.Length > 0)
                    {
                        serialPort.Write(modifiedBytes, 0, modifiedBytes.Length);
                        MessageBox.Show("データを送信しました。");
                    }
                    else
                    {
                        MessageBox.Show("シリアルポートが開いていません。");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"エラーが発生しました: {ex.Message}");
                }
            }
        }

        //　Localボタンを描画する
        private async void Local_button11(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "WAVファイル (*.wav)|*.wav"; // フィルタを設定
                openFileDialog.Title = "WAVファイルを選択";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string sendfilePath = openFileDialog.FileName; // 選択したファイルのパス
                    string outputFilePath = "";

                    // 保存先をユーザーに指定させる
                    using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                    {
                        saveFileDialog.Filter = "WAVファイル (*.wav)|*.wav";
                        saveFileDialog.Title = "保存先を指定";
                        saveFileDialog.FileName = "converted_8000Hz.wav";

                        if (saveFileDialog.ShowDialog() == DialogResult.OK)
                        {
                            outputFilePath = saveFileDialog.FileName;

                            // サンプリングレートを8000Hzに変換
                            ConvertWavFileSampleRate(sendfilePath, outputFilePath);
                        }
                    }

                    try
                    {
                        // サンプリングレートを8000Hzに変換したファイルを読み込む
                        byte[] fileBytes = await Task.Run(() => File.ReadAllBytes(outputFilePath));

                        // 読み込んだバイナリデータの最初の512バイトを表示
                        // int displayLength = Math.Min(fileBytes.Length, 512); // 最初の512バイトだけ取得
                        // string firstHexString = BitConverter.ToString(fileBytes.Take(displayLength).ToArray());

                        // 読み込んだバイナリデータの最後の512バイトを表示
                        // byte[] lastBytes = fileBytes.Skip(Math.Max(0, fileBytes.Length - 512)).ToArray();
                        // string lastHexString = BitConverter.ToString(lastBytes);

                        // メッセージボックスに表示
                        // MessageBox.Show($"最初の512バイトの16進数形式:\n{firstHexString}");
                        // MessageBox.Show($"最後の512バイトの16進数形式:\n{lastHexString}");

                        // バイナリの先頭に指定したバイトを追加するための配列を作成
                        byte[] modifiedBytes = new byte[fileBytes.Length + 11]; // 5 + 2（0x00 と 0xFF）
                        modifiedBytes[0] = 0x53; // 'S'
                        modifiedBytes[1] = 0x54; // 'T'
                        modifiedBytes[2] = 0x41; // 'A'
                        modifiedBytes[3] = 0x52; // 'R'
                        modifiedBytes[4] = 0x54; // 'T'

                        // WAVファイルのバイナリを modifiedBytes の 5 バイト目から追加
                        Array.Copy(fileBytes, 0, modifiedBytes, 5, fileBytes.Length);

                        // 末尾に追加のバイトを設定
                        int startIndex = 5 + fileBytes.Length; // 追加バイトの開始インデックス
                        modifiedBytes[startIndex] = 0x45; // 'E'
                        modifiedBytes[startIndex + 1] = 0x4E; // 'N'
                        modifiedBytes[startIndex + 2] = 0x44; // 'D'
                        modifiedBytes[startIndex + 3] = 0x49; // 'I'
                        modifiedBytes[startIndex + 4] = 0x4E; // 'N'
                        modifiedBytes[startIndex + 5] = 0x47; // 'G'

                        // 変更後のバイナリデータを最初の512バイトを表示
                        // int displayLength2 = Math.Min(modifiedBytes.Length, 512); // 最初の512バイトだけ表示
                        // string modifiedHexString = BitConverter.ToString(modifiedBytes.Take(displayLength2).ToArray());

                        // 変更後バイナリデータの最後の512バイトを表示
                        // byte[] lastBytes2 = modifiedBytes.Skip(Math.Max(0, modifiedBytes.Length - 512)).ToArray();
                        // string lastHexString2 = BitConverter.ToString(lastBytes2);

                        // メッセージボックスに表示
                        // MessageBox.Show($"変更後の512ファイルの16進数形式:\n{modifiedHexString}");
                        // MessageBox.Show($"変更後の512バイトの16進数形式:\n{lastHexString2}");

                        // ファイル保存ダイアログを表示
                        using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                        {
                            saveFileDialog.Filter = "Text files (*.txt)|*.txt"; // テキストファイルのみ
                            saveFileDialog.Title = "バイナリデータの保存先を指定してください";
                            saveFileDialog.FileName = "binary_output.txt"; // デフォルトのファイル名

                            // ユーザーが保存先を指定した場合のみ処理を進める
                            if (saveFileDialog.ShowDialog() == DialogResult.OK)
                            {
                                // バイナリデータをテキストファイルに保存
                                string hexData = BitConverter.ToString(modifiedBytes).Replace("-", " "); // バイナリデータを16進数形式に変換
                                await File.WriteAllTextAsync(saveFileDialog.FileName, hexData); // テキストファイルに保存
                                MessageBox.Show($"バイナリデータを {saveFileDialog.FileName} に保存しました。");
                            }
                            else
                            {
                                MessageBox.Show("ファイル保存がキャンセルされました。");
                            }
                        }

                        // CR通信で送信
                        if (serialPort.IsOpen && modifiedBytes.Length > 0)
                        {
                            serialPort.Write(modifiedBytes, 0, modifiedBytes.Length);
                            MessageBox.Show("データを送信しました。");
                        }
                        else
                        {
                            MessageBox.Show("シリアルポートが開いていません。");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"エラーが発生しました: {ex.Message}");
                    }
                }
            }
        }

        private async void ConvertWavFileSampleRate(string inputFilePath, string outputFilePath)
        {
            try
            {
                // 入力ファイルの読み込み
                using (WaveFileReader reader = new WaveFileReader(inputFilePath))
                {
                    // 変換後のフォーマットを8000Hz、8ビット、モノラルに設定
                    WaveFormat newFormat = new WaveFormat(8000, 8, 1);

                    // サンプリングレートの変換
                    using (WaveFormatConversionStream conversionStream = new WaveFormatConversionStream(newFormat, reader))
                    {
                        // 変換後のWAVファイルを保存
                        WaveFileWriter.CreateWaveFile(outputFilePath, conversionStream);
                    }
                }

                // 変換が完了したことをユーザーに通知
                MessageBox.Show($"ファイルのサンプリングレートを8000Hzに変換しました: {outputFilePath}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"エラーが発生しました: {ex.Message}");
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // シリアルポートを閉じる
            if (serialPort.IsOpen)
            {
                serialPort.Close();
            }
            base.OnFormClosed(e);
        }

        // TTS関連　//
        private string filename;
        private int fileindex;

        // Synthesizeボタンを描画する
        private async void Synthesize_button8(object sender, EventArgs e)
        {
            button8.Enabled = false;

            if (!IsVoiceVoxRunning())
            {
                StartVoiceVox();
                await WaitForVoiceVoxToStart();
            }

            filename = Convert.ToString(fileindex);
            filename += "_" + textBox1.Text;

            string text = textBox1.Text;
            string speakerid = Getspeechsynthesischaracter(comboBox1.Text); // 話者IDを取得
            filename += "_" + comboBox1.Text;
            string audioQuery = await GetAudioQuery(text, speakerid);
            if (audioQuery != null)
            {
                await SynthesizeSpeech(audioQuery, speakerid);
            }

            // 音声ファイルをタイムラインに追加
            AddToTimeline(filename + ".wav");

            fileindex++;
            button8.Enabled = true;
        }

        // ここから VOICEVOX 用関数 
        private bool IsVoiceVoxRunning()
        {
            var processes = Process.GetProcessesByName("run");
            return processes.Length > 0;
        }

        private void StartVoiceVox()
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Programs\VOICEVOX\VOICEVOX.exe"
            };
            Process.Start(startInfo);
        }

        private async Task WaitForVoiceVoxToStart()
        {
            while (!IsVoiceVoxRunning())
            {
                await Task.Delay(1000);
            }
            await Task.Delay(2000);
        }

        private string Getspeechsynthesischaracter(string speakername)
        {
            Dictionary<string, string> speakerids = new Dictionary<string, string>()
            {
                {"四国めたん","2"},
                {"ずんだもん","3"},
                {"春日部つむぎ","8"},
                {"雨晴はう","10"},
                {"波音リツ","9"},
                {"玄野武宏","11"},
                {"白上虎太郎","12"},
                {"青山龍星","13"},
                {"冥鳴ひまり","14"},
                {"九州そら","16"},
                {"もち子さん","20"},
                {"剣崎雌雄","21"},
                {"WhiteCUL","23"},
                {"後鬼","27"},
                {"No.7","29"},
                {"ちび式じい","42"},
                {"櫻歌ミコ","43"},
                {"小夜/SAYO","46"},
                {"ナースロボ＿タイプＴ","47"},
                {"†聖騎士 紅桜†","51"},
                {"雀松朱司","52"},
                {"麒ヶ島宗麟","53"},
                {"春歌ナナ","54"},
                {"猫使アル","55"},
                {"猫使ビィ","58"},
                {"中国うさぎ","61"},
                {"栗田まろん","67"},
                {"あいえるたん","68"},
                {"満別花丸","69"},
                {"琴詠ニア","74"}
            };

            // speakernameが空またはnullの場合のチェック
            if (string.IsNullOrEmpty(speakername))
            {
                MessageBox.Show("話者名が選択されていません。");
                return string.Empty; // またはデフォルトの話者IDを返す
            }

            // 話者名が辞書に存在するかを確認
            if (speakerids.ContainsKey(speakername))
            {
                return speakerids[speakername];
            }
            else
            {
                MessageBox.Show("指定された話者名が見つかりません: " + speakername);
                return string.Empty; // またはデフォルトの話者IDを返す
            }
        }

        private async Task<string> GetAudioQuery(string text, string speakerid)
        {
            var response = await client.PostAsync($"{VOICEVOXurl}/audio_query?text={text}&speaker={speakerid}", null);
            if (response.IsSuccessStatusCode)
            {
                var query = await response.Content.ReadAsStringAsync();

                // JSONデータをオブジェクトに変換
                dynamic queryJson = Newtonsoft.Json.JsonConvert.DeserializeObject(query);

                // パラメータ設定
                queryJson.speedScale = Convert.ToDouble(textBox2.Text);
                queryJson.pitchScale = Convert.ToDouble(textBox3.Text);
                queryJson.intonationScale = Convert.ToDouble(textBox4.Text);

                // オブジェクトをJSONデータに再変換
                return Newtonsoft.Json.JsonConvert.SerializeObject(queryJson);
            }
            return null;
        }

        // SynthesizeSpeech メソッドで音声ファイルを保存した後、AddToTimeline メソッドを呼び出す
        private async Task SynthesizeSpeech(string audioQuery, string speakerid)
        {
            var content = new StringContent(audioQuery, Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{VOICEVOXurl}/synthesis?speaker={speakerid}", content);
            if (response.IsSuccessStatusCode)
            {
                byte[] audioData = await response.Content.ReadAsByteArrayAsync();

                // 音声データを保存
                using (var fileStream = System.IO.File.Create(filename + ".wav"))
                {
                    using (var httpStream = await response.Content.ReadAsStreamAsync())
                    {
                        httpStream.CopyTo(fileStream);
                        fileStream.Flush();
                    }
                }
            }
        }

        //　TextEntryボックスを描画する
        private void TextEntry_textBox1(object sender, EventArgs e)
        {

        }

        //　TextEntryボックスを描画する
        private void TTSCharacter_comboBox1(object sender, EventArgs e)
        {

        }

        //　ReadingSpeedボックスを描画する
        private void ReadingSpeed_textBox2(object sender, EventArgs e)
        {

        }

        //　ReadingSpeedバーを描画する
        private void ReadingSpeed_trackBar1(object sender, EventArgs e)
        {
            textBox2.Text = Convert.ToString(trackBar1.Value / 4f);   // 読み上げ速度
        }

        //　VoiceHeightボックスを描画する
        private void VoiceHeight_textBox3(object sender, EventArgs e)
        {

        }

        //　VoiceHeightバーを描画する
        private void VoiceHeight_trackBar2(object sender, EventArgs e)
        {
            textBox3.Text = Convert.ToString(trackBar2.Value / 20f);  // 声の高さ
        }

        //　Intonationボックスを描画する
        private void textBox4_TextChanged(object sender, EventArgs e)
        {

        }

        //　Intonationバーを描画する
        private void Intonation_trackBar3(object sender, EventArgs e)
        {
            textBox4.Text = Convert.ToString(trackBar3.Value * 0.2f); // 抑揚
        }

        //　TTSグループボックスを描画する
        private void TTS_groupBox1(object sender, EventArgs e)
        {

        }

        // VC関連　//
        private string Getvoicechangercharacter(string speakername)
        {
            Dictionary<string, string> speakerids = new Dictionary<string, string>()
            {
                {"ずんだもん","zundamon-1"},
                {"あみたろ","Amitaro_Zero_100e_3700s"}
            };
            return speakerids[speakername];
        }

        private bool recording = false;
        WaveInEvent VCwaveIn;
        WaveFileWriter VCwaveWriter;

        //　
        private void Recording_button9(object sender, EventArgs e)
        {
            if (!recording)
            {
                var deviceNumber = 0;

                // 録画処理を開始
                VCwaveIn = new WaveInEvent();
                VCwaveIn.DeviceNumber = deviceNumber;
                VCwaveIn.WaveFormat = new WaveFormat(48000, WaveIn.GetCapabilities(deviceNumber).Channels);

                VCwaveWriter = new WaveFileWriter(".\\" + "record_temp.wav", VCwaveIn.WaveFormat);

                VCwaveIn.DataAvailable += (_, ee) =>
                {
                    VCwaveWriter.Write(ee.Buffer, 0, ee.BytesRecorded);
                    VCwaveWriter.Flush();
                };

                VCwaveIn.StartRecording();
                button9.Text = "Stop";
                recording = true;
            }
            else
            {
                VCwaveIn?.StopRecording();
                VCwaveIn?.Dispose();
                VCwaveIn = null;

                VCwaveWriter?.Close();
                VCwaveWriter = null;
                button9.Enabled = false;
                button9.Text = "Processing";

                // ボイスチェンジ開始
                string voice = Getvoicechangercharacter(comboBox2.Text);
                string pitch = textBox5.Text;
                filename = fileindex + "_vc.wav";
                string command = "call venv\\Scripts\\activate & python -m rvc_python -i " + "record_temp.wav" + " -mp .\\voice\\" + voice + ".pth " + "-pi " + pitch + " -me rmvpe -v v2 " + "-o " + filename + " --device cpu";
                StreamWriter sw = new StreamWriter("temp.bat", false);
                sw.WriteLine(command);
                sw.Close();
                Process p = Process.Start("temp.bat");
                p.WaitForExit();

                File.Delete("temp.bat");

                string filePath = filename;

                // ファイルをタイムラインに追加
                AddToTimeline(filePath);

                button9.Text = "Record";
                button9.Enabled = true;
                recording = false;
                fileindex++;
            }
        }

        private void VCVoiceHeight_textBox5(object sender, EventArgs e)
        {

        }

        private void VCVoiceHeight_trackBar4(object sender, EventArgs e)
        {
            textBox5.Text = trackBar4.Value.ToString();
        }

        private void VCCharacter_comboBox2(object sender, EventArgs e)
        {

        }

        //　VCグループボックスを描画する
        private void VC_groupBox2(object sender, EventArgs e)
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

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }
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

        //　タイムライン全体の長さを取得する
        public TimeSpan TotalDuration
        {
            get
            {
                if (_objects.Count == 0)
                {
                    return TimeSpan.Zero;
                }

                var firstStart = _objects.Min(o => o.StartTime);
                var lastEnd = _objects.Max(o => o.StartTime + o.Duration);

                return lastEnd - firstStart;
            }
        }

        // IsSelected = trueのオブジェクトを取得する
        public List<TimelineObject> GetSelectedObjects()
        {
            return _objects.Where(o => o.IsSelected).ToList();
        }
    }

    // オブジェクトのプロパティを保持する
    public class TimelineObject
    {
        private TimeSpan _startTime;
        private WaveOffsetStream _waveOffsetStream; // WaveOffsetStreamを使用する
        public TimeSpan Duration { get; set; }
        public TimeSpan EndTime => StartTime + Duration;
        public int Layer { get; set; }
        public string FilePath { get; set; }
        public bool IsSelected { get; set; } // 選択状態
        public Point DrawingPosition { get; set; }　// 描画座標を保持する

        public string FileName => Path.GetFileName(FilePath);

        //　初期化
        public TimelineObject(TimeSpan startTime, WaveOffsetStream waveOffsetStream, TimeSpan duration, int layer, string filePath)
        {
            _startTime = startTime;
            _waveOffsetStream = waveOffsetStream;
            Duration = duration;
            Layer = layer;
            FilePath = filePath;
            IsSelected = false; // 初期状態では選択されていない
        }

        // StartTimeプロパティ
        public TimeSpan StartTime
        {
            get => _startTime;
            set
            {
                _startTime = value;
                if (_waveOffsetStream != null)
                {
                    // StartTimeの変更をWaveOffsetStreamに反映
                    _waveOffsetStream.StartTime = _startTime;
                }
            }
        }
    }

    // 再生関連の機能を提供
    public class AudioPlayer
    {
        private Timeline _timeline;

        private List<CustomWavePlayer> _wavePlayers;
        private List<CustomWaveStream> _waveStreams;
        public List<CustomWaveOffsetStream> _waveOffsetStreams;
        private Dictionary<WaveStream, IWavePlayer> _waveStreamPlayerMap;
        private Dictionary<string, WaveStream> _filePathWaveStreamMap;
        private Dictionary<string, WaveOffsetStream> _filePathWaveOffsetStreamMap;

        public WaveStream waveStream { get; set; }
        public IWavePlayer wavePlayer { get; set; }

        // 初期化
        public AudioPlayer()
        {
            _timeline = new Timeline();

            _wavePlayers = new List<CustomWavePlayer>();
            _waveStreams = new List<CustomWaveStream>();
            _waveOffsetStreams = new List<CustomWaveOffsetStream>();
            _filePathWaveStreamMap = new Dictionary<string, WaveStream>();
            _waveStreamPlayerMap = new Dictionary<WaveStream, IWavePlayer>();
            _filePathWaveOffsetStreamMap = new Dictionary<string, WaveOffsetStream>();
        }

        //　指定されたファイルパスからオーディオファイルを読み込む
        public TimelineObject Load(string filePath)
        {
            var wavePlayer = new CustomWavePlayer(); // 引数なしのコンストラクターを使用
            var waveStream = new AudioFileReader(filePath);

            // PCM変換処理：MediaFoundationReaderで読み込み、PCMフォーマットに変換
            var reader = new MediaFoundationReader(filePath);
            var pcmStream = WaveFormatConversionStream.CreatePcmStream(reader);

            // WaveOffsetStreamの作成
            var waveOffsetStream = new CustomWaveOffsetStream(pcmStream, TimeSpan.Zero, TimeSpan.Zero, pcmStream.TotalTime);

            wavePlayer.Init(waveOffsetStream); // CustomWavePlayerを初期化
            _wavePlayers.Add(wavePlayer);
            _waveOffsetStreams.Add(waveOffsetStream); // オフセットストリームをリストに追加

            // waveOffsetStream を CustomWaveStream に変換
            var customWaveStream = new CustomWaveStream(waveOffsetStream);

            _waveStreams.Add(customWaveStream); // waveStream をリストに追加
            _filePathWaveStreamMap[filePath] = customWaveStream; // waveStream をマップに追加
            _waveStreamPlayerMap[customWaveStream] = wavePlayer;
            _filePathWaveOffsetStreamMap[filePath] = waveOffsetStream; // offsetStream をマップに追加

            // Durationは、waveStreamから取得する
            TimeSpan duration = customWaveStream.TotalTime;

            // TimelineObjectを作成して情報を格納
            var timelineObject = new TimelineObject(
                startTime: TimeSpan.Zero,
                waveOffsetStream: waveOffsetStream,
                duration: duration,
                layer: 0,
                filePath: filePath
            )
            {
                IsSelected = false // 初期状態では選択されていないものとする
            };

            return timelineObject;
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
            foreach (var offsetstream in _waveOffsetStreams)
            {
                offsetstream.Dispose();
            }

            _wavePlayers.Clear();
            _waveStreams.Clear();
            _waveOffsetStreams.Clear();
        }

        // 選択されたオブジェクトに関連する WaveStream を削除する
        public void Delete(List<TimelineObject> selectedObjects)
        {
            foreach (var obj in selectedObjects)
            {
                // TimelineObject に紐付けられているファイルパスから WaveStream を取得
                var filePath = obj.FilePath;

                // ファイルパスに基づいて WaveStream をリストから検索
                if (_filePathWaveStreamMap.TryGetValue(filePath, out var waveStream) && waveStream is CustomWaveStream customWaveStream)
                {
                    // wavePlayer の取得
                    if (_waveStreamPlayerMap.TryGetValue(waveStream, out var wavePlayer) && wavePlayer is CustomWavePlayer customWavePlayer)
                    {
                        customWavePlayer.Stop(); // 再生を停止
                        customWavePlayer.Dispose(); // WavePlayer を解放
                        _wavePlayers.Remove(customWavePlayer); // _wavePlayers リストから削除
                        _waveStreamPlayerMap.Remove(waveStream); // マップから削除

                        // WaveStream を解放し、リストから削除
                        customWaveStream.Dispose();
                        _waveStreams.Remove(customWaveStream); // _waveStreams リストから削除
                        _filePathWaveStreamMap.Remove(filePath); // _filePathWaveStreamMap から削除
                    }
                }
            }
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

    //　WaveOffsetStream のラッパークラス
    public class CustomWaveOffsetStream : WaveOffsetStream, IDisposable
    {
        public bool IsDisposed { get; private set; }

        public CustomWaveOffsetStream(WaveStream source, TimeSpan startTime, TimeSpan endTime, TimeSpan totalTime)
            : base(source, startTime, endTime, totalTime)
        {
            IsDisposed = false;
        }

        public CustomWaveStream ToCustomWaveStream()
        {
            // WaveStream を CustomWaveStream に変換
            return new CustomWaveStream(this); // このクラスが WaveStream を継承しているため、直接使用可能
        }

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    // マネージリソースの解放
                    base.Dispose(disposing);
                }

                // 非マネージリソースの解放があればここに記述

                IsDisposed = true;
            }
            base.Dispose(disposing);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    //　WaveStream のラッパークラス
    public class CustomWaveStream : WaveStream
    {
        private readonly WaveStream _sourceStream;

        public CustomWaveStream(WaveStream sourceStream)
        {
            _sourceStream = sourceStream ?? throw new ArgumentNullException(nameof(sourceStream));
        }

        public override long Length => _sourceStream.Length;

        public override long Position
        {
            get => _sourceStream.Position;
            set => _sourceStream.Position = value;
        }

        public override WaveFormat WaveFormat => _sourceStream.WaveFormat;

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _sourceStream.Read(buffer, offset, count);
        }

        public override TimeSpan CurrentTime => TimeSpan.FromMilliseconds(Position * 1000.0 / WaveFormat.AverageBytesPerSecond);

        // Disposeメソッドなど他の必要なメソッドも実装
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _sourceStream?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    //　IWavePlayer のラッパークラス
    public class CustomWavePlayer : IWavePlayer, IDisposable
    {
        private readonly WaveOutEvent _waveOut;
        public IWavePlayer Player { get; private set; }
        public bool IsDisposed { get; private set; }

        public event EventHandler<StoppedEventArgs> PlaybackStopped
        {
            add { _waveOut.PlaybackStopped += value; }
            remove { _waveOut.PlaybackStopped -= value; }
        }

        public float Volume
        {
            get => _waveOut.Volume; // WaveOutEventのVolumeを返す
            set => _waveOut.Volume = value; // WaveOutEventのVolumeを設定
        }

        // 引数なしのコンストラクタ
        public CustomWavePlayer()
        {
            _waveOut = new WaveOutEvent();
            IsDisposed = false;
        }

        // 引数ありのコンストラクタ
        public CustomWavePlayer(IWavePlayer player)
        {
            Player = player;
            _waveOut = new WaveOutEvent(); // WaveOutEventの初期化
            IsDisposed = false;
        }

        public void Init(IWaveProvider waveProvider)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(CustomWavePlayer)); // Disposeされている場合は例外をスロー

            _waveOut.Init(waveProvider);
        }

        public void Play()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(CustomWavePlayer)); // Disposeされている場合は例外をスロー

            _waveOut.Play();
        }

        public void Pause()
        {
            _waveOut.Pause();
        }

        public void Stop()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(CustomWavePlayer)); // Disposeされている場合は例外をスロー

            _waveOut.Stop(); // WaveOutEventのStopメソッドを呼び出す
        }

        public PlaybackState PlaybackState => _waveOut.PlaybackState;

        public WaveFormat OutputWaveFormat => _waveOut.OutputWaveFormat;

        public void Dispose()
        {
            if (Player != null && !IsDisposed)
            {
                Player.Dispose();
                IsDisposed = true;
            }
        }
    }
}
