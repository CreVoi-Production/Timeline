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
        private TimeSpan _currentPlaybackTime; //�@���݂̍Đ����Ԃ�ێ�����
        private bool _isPlaying; //�@�Đ���Ԃ�����
        private TimelineObject _selectedObject; //�@�I������Ă���I�u�W�F�N�g�̕ێ�����
        private Point _dragStartPoint; //�@�h���b�O�̊J�n�ʒu��ێ�����
        private bool _isDragging; //�@�h���b�O�����Ԃ�����
        private int numberOfLayers = 100; // ���C���[���̏����l
        private int layerHeight = 50; // �e���C���[�̍���
        private double pixelsPerMillisecond => (double)panel1.ClientSize.Width / _timeline.TotalDuration.TotalMilliseconds; // 1�~���b������̃s�N�Z�����i���Ԏ��̃X�P�[���j�@
        private WaveInEvent waveIn;
        private WaveFileWriter waveFileWriter;
        private bool isRecording = false;
        private SerialPort serialPort;
        private string sendfilePath;

        private static readonly HttpClient client = new HttpClient();   // VOICEVOX �N���C�A���g
        private const string VOICEVOXurl = "http://127.0.0.1:50021";    // VOICEVOX �T�[�o�[�A�h���X

        //�@������
        public Form1()
        {
            InitializeComponent();

            _timeline = new Timeline();
            _audioPlayer = new AudioPlayer();

            _playbackTimer = new System.Windows.Forms.Timer();
            _playbackTimer.Interval = 100; // �~���b�P�ʁA�����ł�100ms���ƂɍX�V
            _playbackTimer.Tick += PlaybackTimer_Tick;

            // TrackBar �̏����ݒ�
            UpdateTrackBar(TimeSpan.Zero, TimeSpan.FromSeconds(10)); // 10�b�̃^�C�����C��

            // �h���b�O���h���b�v�̐ݒ�
            this.AllowDrop = true;
            this.DragEnter += Form1_DragEnter;
            this.DragDrop += Form1_DragDrop;

            //�I�u�W�F�N�g�̃h���b�O�ړ�
            panel1.MouseDown += TimelinePanel_MouseDown;
            panel1.MouseMove += TimelinePanel_MouseMove;
            panel1.MouseUp += TimelinePanel_MouseUp;

            serialPort = new SerialPort();

            // ���p�\��COM�|�[�g���擾
            string[] availablePorts = SerialPort.GetPortNames();

            // ���p�\�ȃ|�[�g��\���i�f�o�b�O��I��p�j
            //foreach (string port in availablePorts)
            //{
            //    MessageBox.Show("Available Port: " + port);
            //}

            // �g�p����|�[�g���Z�b�g
            if (availablePorts != null && availablePorts.Length > 0)
            {
                // �V���A���|�[�g�̏����ݒ�
                serialPort = new SerialPort
                {
                    BaudRate = 115200,         // �{�[���[�g��115200bps�ɐݒ�
                    Parity = Parity.None,       // �p���e�B���Ȃ��ɐݒ�
                    DataBits = 8,               // �f�[�^�r�b�g��8�ɐݒ�
                    StopBits = StopBits.One,    // �X�g�b�v�r�b�g��1�ɐݒ�
                    PortName = availablePorts[0]           // COM�|�[�g����ݒ� (���ۂ̊��ɍ��킹�ĕύX)
                };
            }
            else
            {
                MessageBox.Show("���p�\�ȃV���A���|�[�g������܂���B", "�G���[", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            try
            {
                serialPort.Open(); // �V���A���|�[�g���J��
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show("���̃v���Z�X���|�[�g���g�p���Ă���\��������܂�: " + ex.Message);
            }
            catch (FileNotFoundException ex)
            {
                MessageBox.Show("�w�肵���|�[�g�����݂��܂���: " + ex.Message);
            }
            catch (IOException ex)
            {
                MessageBox.Show("�V���A���|�[�g�̓��o�̓G���[���������܂���: " + ex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show("�\�����Ȃ��G���[���������܂���: " + ex.Message);
            }
        }

        //�@Timeline��`�悷��
        private void Timeline_panel1(object sender, PaintEventArgs e)
        {
            // �^�C�����C���I�u�W�F�N�g��`�悷�鏈��
            int scrollOffset = hScrollBar1.Value;
            foreach (var obj in _timeline.GetObjects())
            {
                DrawTimelineObject(e.Graphics, obj, scrollOffset);
            }

            Graphics g = e.Graphics;

            // ���C���[����`�悷��
            DrawLayerLines(g, numberOfLayers);
        }

        //�@�I�u�W�F�N�g���^�C�����C����ɕ`�悷��
        private void DrawTimelineObject(Graphics g, TimelineObject obj, int scrollOffset)
        {
            // �I�u�W�F�N�g�̈ʒu�ƃT�C�Y���v�Z���ĕ`��
            int x = (int)(obj.StartTime.TotalMilliseconds * pixelsPerMillisecond) - scrollOffset;  // �~���b���s�N�Z���ɕϊ�
            int width = (int)(obj.Duration.TotalMilliseconds * pixelsPerMillisecond);  // �~���b���s�N�Z���ɕϊ�
            int y = obj.Layer * layerHeight;  // ���C���[���Ƃ�Y���W��ς���
            int height = layerHeight - 10; // �I�u�W�F�N�g�̍�����ݒ�

            // �`����W���I�u�W�F�N�g�ɐݒ�
            obj.DrawingPosition = new Point(x, y);

            // �I������Ă��邩�ǂ����ɉ����ĐF��ύX
            Brush brush = obj.IsSelected ? Brushes.Red : Brushes.Blue;

            // �I�u�W�F�N�g�̎l�p�`��`��
            g.FillRectangle(brush, x, y, width, height);
            g.DrawRectangle(Pens.Black, x, y, width, height);

            // �I�u�W�F�N�g���i�t�@�C�����j��`��
            g.DrawString(obj.FileName, SystemFonts.DefaultFont, Brushes.White, x + 5, y + 5);
        }

        // ���C���[�Ԃւ̐��̕`��
        private void DrawLayerLines(Graphics g, int numberOfLayers)
        {
            // ���C���[�̍���
            int layerHeight = 50;

            // ���̐F�ƃX�^�C����ݒ�
            Pen pen = new Pen(Color.Gray, 1); // �O���[�̐��A����1�s�N�Z��

            // �e���C���[�Ԃɐ�������
            for (int i = 1; i < numberOfLayers; i++)
            {
                int y = i * layerHeight;
                g.DrawLine(pen, 0, y, this.Width, y); // ���̗�ł́APanel�̕��S�̂ɐ��������܂�
            }
        }

        //�@���Ԃ��s�N�Z���ʒu�ɕϊ�����
        private int TimeToPixel(TimeSpan time)
        {
            return (int)(time.TotalMilliseconds * pixelsPerMillisecond);
        }

        //�@�s�N�Z���ʒu�����Ԃɕϊ�����
        private TimeSpan PixelToTime(int pixel)
        {
            return TimeSpan.FromMilliseconds(pixel / pixelsPerMillisecond);
        }

        //�@PaintEventArgs ���g�p���ăJ�X�^���`�������
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;

            // �^�C�����C���I�u�W�F�N�g��`��
            var objects = _timeline.GetObjects();
            int scrollOffset = hScrollBar1.Value;
            foreach (var obj in objects)
            {
                DrawTimelineObject(g, obj, scrollOffset);
            }

            // ���C���[�����擾���Đ���`��
            if (objects.Any())  // ���X�g����łȂ����Ƃ��m�F
            {
                int numberOfLayers = objects.Max(o => o.Layer) + 1;
                DrawLayerLines(g, numberOfLayers);
            }

            // ���ԃ��x����`�悷��
            DrawTimeLabels(g);
        }

        // �^�C�����C���̎��ԃ��x����`�悷��@
        private void DrawTimeLabels(Graphics graphics)
        {
            if (_timeline.TotalDuration == TimeSpan.Zero)
                return;

            // �^�C�����C���̑S�̂ɂ킽�鎞�Ԃ̃��x����`��
            double PixelsPerMillisecond = pixelsPerMillisecond;
            TimeSpan duration = _timeline.TotalDuration;
            int numberOfLabels = (int)(duration.TotalMinutes) + 1; // ���P�ʂŃ��x����ݒ�

            Font font = new Font("Arial", 8);
            Brush brush = Brushes.Black;

            for (int i = 0; i <= numberOfLabels; i++)
            {
                TimeSpan labelTime = TimeSpan.FromMinutes(i * 1); // 1���P�ʂ̃��x��
                int xPosition = TimeToPixel(labelTime);

                // ���x���e�L�X�g�̕`��
                string timeLabel = labelTime.ToString(@"hh\:mm\:ss");
                graphics.DrawString(timeLabel, font, brush, xPosition, 0);
            }
        }

        //�@�t�@�C�����h���b�O���ăt�H�[���ɓ������ۂɁA�h���b�O&�h���b�v�ŋ�����邩���f����
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

        //�@�h���b�O&�h���b�v�����ۂɁA���[�h���ă^�C�����C���ɒǉ�����
        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            var filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (var filePath in filePaths)
            {
                // �����t�@�C���̃��[�h
                TimelineObject timelineObject = _audioPlayer.Load(filePath);

                // �����t�@�C�������������[�h����Ă��邩�m�F
                if (_audioPlayer.TotalTime != TimeSpan.Zero)
                {
                    // �^�C�����C���I�u�W�F�N�g�Ƃ��Ēǉ�
                    _timeline.AddObject(timelineObject);

                    // �^�C�����C���̒����������t�@�C���̒����ɍ��킹�čX�V
                    UpdateTrackBar(TimeSpan.Zero, _audioPlayer.TotalTime);

                    // �^�C�����C���̏I�����Ԃ��X�V
                    UpdateTimelineEndTime();

                    // �^�C�����C���̊J�n���Ԃ��X�V
                    DisplayFirstObjectStartTime();

                    // �^�C�����C�����ĕ`��
                    panel1.Invalidate();
                }
                else
                {
                    MessageBox.Show($"�����t�@�C���̓ǂݍ��݂Ɏ��s���܂���: {filePath}", "�G���[", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        //�@�}�E�X�N���b�N���s��ꂽ�ۂɁA�I�u�W�F�N�g��I�����ăh���b�O������J�n����
        private void TimelinePanel_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) // ���N���b�N�Ńh���b�O���J�n
            {
                // �S�ẴI�u�W�F�N�g�̑I����Ԃ��N���A
                foreach (var obj in _timeline.GetObjects())
                {
                    obj.IsSelected = false; // �I������
                }

                // �N���b�N���ꂽ�ʒu�ɃI�u�W�F�N�g�����邩�m�F
                foreach (var obj in _timeline.GetObjects())
                {
                    // `DrawingPosition` �𗘗p���ăI�u�W�F�N�g�̋�`���v�Z
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

        //�@�I�u�W�F�N�g���h���b�O���Ĉړ�����ۂɁA�J�n���Ԃƃ��C���[���X�V���ă^�C�����C�����ĕ`�悷��
        private void TimelinePanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && _selectedObject != null)
            {
                // �}�E�X�̈ړ��ʂ��v�Z
                int deltaX = e.X - _dragStartPoint.X;
                int deltaY = e.Y - _dragStartPoint.Y;

                // �X�P�[�����O�t�@�N�^�[��ݒ�
                double scaleFactor = 1;

                // 1�s�N�Z��������̎��ԒP�ʂ�ݒ� (1�~���b������1�s�N�Z���j
                double pixelsPerMillisecond = 1;

                // �X�P�[�����O�t�@�N�^�[��K�p
                double scaledDeltaX = deltaX * scaleFactor;
                double scaledDeltaY = deltaY * scaleFactor;

                // �Đ��J�n���Ԃ��X�V�i�ړ��ʂɊ�Â��ĊJ�n���Ԃ�ύX�j
                TimeSpan newStartTime = _selectedObject.StartTime + TimeSpan.FromMilliseconds(deltaX * pixelsPerMillisecond);
                if (newStartTime < TimeSpan.Zero) newStartTime = TimeSpan.Zero;  // �J�n���Ԃ�0�����ɂ��Ȃ�

                // �J�n���Ԃ����̒l�ɂȂ�Ȃ��悤�ɐ���
                if (newStartTime < TimeSpan.Zero) newStartTime = TimeSpan.Zero;

                // �I�u�W�F�N�g�̊J�n���Ԃ��X�V
                _selectedObject.StartTime = newStartTime;

                // �h���b�O�J�n�ʒu���X�V
                _dragStartPoint = e.Location;

                // �V�������C���[���v�Z
                int newLayer = _selectedObject.Layer + deltaY / 20;
                if (newLayer < 0) newLayer = 0;  // ���C���[��0�����ɂ��Ȃ�

                // �X�V�����l��K�p
                _selectedObject.StartTime = newStartTime;
                _selectedObject.Layer = newLayer;

                // �`����W���Čv�Z���čX�V
                _selectedObject.DrawingPosition = new Point(
                    (int)(newStartTime.TotalMilliseconds * pixelsPerMillisecond) - hScrollBar1.Value,
                    _selectedObject.Layer * layerHeight
                );

                // �ł������J�n���Ԃ�\��
                DisplayFirstObjectStartTime();

                // �^�C�����C���̏I�����Ԃ��X�V
                UpdateTimelineEndTime();

                // �����v���[���[�̊J�n�ʒu���X�V
                UpdateAudioPlayerStartTime(_selectedObject);

                // �^�C�����C�����ĕ`��
                panel1.Invalidate();
            }
        }

        //�@TimelineObject��StartTime���X�V���AwaveStream��wavePlayer��Ή�����StartTime�Ɋ�Â��čX�V����
        private void UpdateAudioPlayerStartTime(TimelineObject obj)
        {
            // 1�~���b������̃o�C�g����ݒ�i���ۂ̎����ɉ����Ē����j
            const double BytesPerMillisecond = 44.1 * 2 * 2 / 1000; // 44.1kHz �X�e���I 16�r�b�g

            // StartTime ���~���b�ɕϊ�
            double startMilliseconds = obj.StartTime.TotalMilliseconds;

            // waveStream �̍Đ��ʒu��ݒ�
            if (_audioPlayer.waveStream != null)
            {
                long newPosition = (long)(startMilliseconds * BytesPerMillisecond);
                _audioPlayer.waveStream.Position = newPosition;
            }

            // wavePlayer �̍Đ��ʒu��ݒ�
            if (_audioPlayer != null && _audioPlayer.wavePlayer != null)
            {
                _audioPlayer.wavePlayer.Stop();
                _audioPlayer.wavePlayer.Play();
            }
        }

        //�@�h���b�O���I�������ۂɁA�I�u�W�F�N�g�����̃��C���[�ɏd�Ȃ��Ă��邩���m�F���A�d�Ȃ��Ă���ꍇ�͋󂢂Ă��郌�C���[�Ɉړ�����
        private void TimelinePanel_MouseUp(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;

                // ���C���[�̏Փ˂��m�F���A�K�v�ɉ����ă��C���[�𒲐�����
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

        //�@movingObject �� targetLayer �Ɉړ������ꍇ�ɁA���̃I�u�W�F�N�g�Əd�Ȃ��Ă��邩�𔻒�
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

        // 2�̃I�u�W�F�N�g���d�Ȃ��Ă��邩���肷��
        private bool IsOverlapping(TimelineObject obj1, TimelineObject obj2)
        {
            // obj1�̊J�n���Ԃ�obj2�̏I�����Ԃ��O�ŁA����obj1�̏I�����Ԃ�obj2�̊J�n���Ԃ���̏ꍇ�A�d�Ȃ��Ă���
            return obj1.StartTime < obj2.StartTime + obj2.Duration &&
                   obj1.StartTime + obj1.Duration > obj2.StartTime;
        }

        //�@Add�{�^����`�悷��
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
                    // �t�@�C�����^�C�����C���ɒǉ�
                    AddToTimeline(filePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"�t�@�C���̓ǂݍ��݂Ɏ��s���܂���: {ex.Message}");
                }
            }
        }

        // �t�@�C�����^�C�����C���ɒǉ����鋤�ʃ��\�b�h
        private void AddToTimeline(string filePath)
        {
            // �t�@�C���p�X�̃`�F�b�N
            if (string.IsNullOrEmpty(filePath))
            {
                MessageBox.Show("�����ȃt�@�C���p�X�ł��B");
                return;
            }

            // �^�C�����C���I�u�W�F�N�g�̍쐬�ƒǉ�
            TimelineObject timelineObject = _audioPlayer.Load(filePath);
            _timeline.AddObject(timelineObject);

            // �^�C�����C���̒����������t�@�C���̒����ɍ��킹�čX�V
            UpdateTrackBar(TimeSpan.Zero, GetMaximumEndTime());

            // �ł������J�n���Ԃ�\��
            DisplayFirstObjectStartTime();

            // �^�C�����C���̏I�����Ԃ��X�V
            UpdateTimelineEndTime();

            // �^�C�����C�����ĕ`��
            panel1.Invalidate();
        }


        //�@Clean�{�^����`�悷��
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

        //�@Delete�{�^����`�悷��
        private void Delete_button7(object sender, EventArgs e)
        {
            // �I������Ă���I�u�W�F�N�g��T��
            var selectedObjects = _timeline.GetSelectedObjects();

            if (selectedObjects.Count > 0)
            {
                // Timeline���X�g����I�����ꂽ�I�u�W�F�N�g���폜
                foreach (var obj in selectedObjects)
                {
                    _timeline.RemoveObject(obj);
                }

                // WaveStream���X�g����Y������I�u�W�F�N�g���폜
                _audioPlayer.Delete(selectedObjects);

                // �I����Ԃ�����
                _selectedObject = null;

                // �^�C�����C�����ĕ`��
                panel1.Invalidate();
            }
            else
            {
                // �I������Ă���I�u�W�F�N�g���Ȃ��ꍇ
                MessageBox.Show("�폜����I�u�W�F�N�g���I������Ă��܂���B");
            }
        }

        //�@Play�{�^����`�悷��
        private void Play_button2(object sender, EventArgs e)
        {
            if (!_isPlaying)
            {
                _isPlaying = true;
                _audioPlayer.Play();
                _playbackTimer.Start();
            }
        }

        //�@Stop�{�^����`�悷��
        private void Stop_button3(object sender, EventArgs e)
        {
            _isPlaying = false;
            _audioPlayer.Stop();
            _playbackTimer.Stop();
            _currentPlaybackTime = TimeSpan.Zero;
            UpdateTrackBar(_currentPlaybackTime, TimeSpan.FromSeconds(60));
        }

        //�@Reset�{�^����`�悷��
        private void Reset_button4(object sender, EventArgs e)
        {
            _audioPlayer.Reset();
            _isPlaying = false;
            _audioPlayer.Stop();
            _playbackTimer.Stop();
            var currentTime = _audioPlayer.CurrentTime;
            label3.Text = $"Playback Time: {currentTime.ToString(@"hh\:mm\:ss")}";
        }

        //�@Export�{�^����`�悷��(�^����)
        private async void Export_button6(object sender, EventArgs e)
        {
            //using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            //{
            //    saveFileDialog.Filter = "WAV�t�@�C�� (*.wav)|*.wav"; // �t�B���^��ݒ�
            //    saveFileDialog.Title = "WAV�t�@�C����ۑ�";
            //    saveFileDialog.DefaultExt = "wav"; // �f�t�H���g�̊g���q

            //    if (saveFileDialog.ShowDialog() == DialogResult.OK)
            //    {
            //        string outputFilePath = saveFileDialog.FileName;
            //        sendfilePath = saveFileDialog.FileName;

            //        // �^���ƍĐ��̊J�n
            //        StartRecording(outputFilePath);
            //        _audioPlayer.Play();
            //    }
            //}

            // �ۑ��_�C�A���O�̕\��
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "WAV�t�@�C�� (*.wav)|*.wav";
                saveFileDialog.Title = "�ۑ�����w��";
                saveFileDialog.FileName = "timeline_mix_output.wav";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string outputFilePath = saveFileDialog.FileName;

                    // WAV�t�@�C���̏����o�����������s
                    await ExportTimelineMixToWav(outputFilePath);
                }
            }
        }

        // �^�C�����C����̂��ׂĂ�WaveOffsetStream���~�b�N�X����WAV�ɏ����o��
        private async Task ExportTimelineMixToWav(string outputFilePath)
        {
            // �^�C�����C����̃I�t�Z�b�g�X�g���[�����~�b�N�X
            var offsetStreams = _audioPlayer._waveOffsetStreams
                                            .Where(stream => stream != null)
                                            .Select(stream => stream.ToSampleProvider())
                                            .ToList();

            if (!offsetStreams.Any())
            {
                MessageBox.Show("�~�b�N�X�Ώۂ̃I�t�Z�b�g�X�g���[��������܂���B");
                return;
            }

            // MixingSampleProvider�̏�����
            var mixingSampleProvider = new MixingSampleProvider(offsetStreams);

            // �v���O���X�_�C�A���O�̕\��
            var progressDialog = new ExportProgressDialog.Form2();
            progressDialog.Show(); // �v���O���X�_�C�A���O��\��

            // WAV�t�@�C���ɏ����o��
            using (var waveFileWriter = new WaveFileWriter(outputFilePath, mixingSampleProvider.WaveFormat))
            {
                float[] buffer = new float[1024];
                int totalSamples = mixingSampleProvider.WaveFormat.SampleRate * 60; // ����1���Ԃ̃T���v������z��i�K�X�����j
                int processedSamples = 0;

                await Task.Run(() =>
                {
                    int samplesRead;
                    while ((samplesRead = mixingSampleProvider.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        waveFileWriter.WriteSamples(buffer, 0, samplesRead);
                        processedSamples += samplesRead;

                        // �i�����v�Z
                        var progress = (double)processedSamples / totalSamples * 100;

                        // �v���O���X�_�C�A���O���X�V
                        for (progress = 0; progress <= 100; progress += 10)
                        {
                            // �i�����X�V
                            progressDialog.UpdateProgress(progress, $"�i�s��: {progress}%");
                        }
                    }
                });
            }

            // �����o����AMixingSampleProvider������������
            mixingSampleProvider = null;

            // �v���O���X�_�C�A���O�����
            progressDialog.Invoke((MethodInvoker)delegate
            {
                progressDialog.Close();
            });

            MessageBox.Show("WAV�t�@�C���̏����o�����������܂����B");
        }

        // �^�����J�n����
        private void StartRecording(string filePath)
        {
            waveIn = new WaveInEvent();
            waveIn.DeviceNumber = 0; // �f�t�H���g�̃}�C�N
            waveIn.WaveFormat = new WaveFormat(8000, 8, 1); // 8.0kHz�A8bit�A���m����

            waveIn.DataAvailable += OnDataAvailable;
            waveIn.RecordingStopped += OnRecordingStopped;

            waveFileWriter = new WaveFileWriter(filePath, waveIn.WaveFormat);

            _audioPlayer.Reset();
            waveIn.StartRecording();
            _playbackTimer.Start();
            isRecording = true;

            // �Đ��^�C�}�[�̐ݒ�
            _playbackTimer = new System.Windows.Forms.Timer();
            _playbackTimer.Interval = 100; // 100�~���b���ƂɍX�V
            _playbackTimer.Tick += OnPlaybackTick;
            _playbackTimer.Start();
        }

        // �Đ��^�C�}�[��Tick�C�x���g
        private void OnPlaybackTick(object sender, EventArgs e)
        {
            var currentTime = _audioPlayer.CurrentTime;
            var maxEndTime = GetMaximumEndTime();

            // maxEndTime�ɒB�������~
            if (currentTime >= maxEndTime)
            {
                StopRecording();
                StopPlayback();
            }
        }

        // �^�����~����
        private void StopRecording()
        {
            if (isRecording)
            {
                waveIn.StopRecording();
                isRecording = false;
            }
        }

        // �^���f�[�^���t�@�C���ɏ�������
        private void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            if (waveFileWriter != null)
            {
                waveFileWriter.Write(e.Buffer, 0, e.BytesRecorded);
            }
        }

        // �^����~��̏���
        private void OnRecordingStopped(object sender, StoppedEventArgs e)
        {
            waveFileWriter?.Dispose();
            waveFileWriter = null;
        }

        //�@Send�{�^����`�悷��
        private async void Send_button10(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(sendfilePath))
            {
                try
                {
                    // WAV�t�@�C���̃o�C�i����ǂݍ���
                    byte[] fileBytes = await Task.Run(() => File.ReadAllBytes(sendfilePath));

                    // �ǂݍ��񂾃o�C�i���f�[�^�̍ŏ���512�o�C�g��\��
                    // int displayLength = Math.Min(fileBytes.Length, 512); // �ŏ���512�o�C�g�����擾
                    // string firstHexString = BitConverter.ToString(fileBytes.Take(displayLength).ToArray());

                    // �ǂݍ��񂾃o�C�i���f�[�^�̍Ō��512�o�C�g��\��
                    // byte[] lastBytes = fileBytes.Skip(Math.Max(0, fileBytes.Length - 512)).ToArray();
                    // string lastHexString = BitConverter.ToString(lastBytes);

                    // ���b�Z�[�W�{�b�N�X�ɕ\��
                    // MessageBox.Show($"�ŏ���512�o�C�g��16�i���`��:\n{firstHexString}");
                    // MessageBox.Show($"�Ō��512�o�C�g��16�i���`��:\n{lastHexString}");

                    // �o�C�i���̐擪�Ɏw�肵���o�C�g��ǉ����邽�߂̔z����쐬
                    byte[] modifiedBytes = new byte[fileBytes.Length + 11]; // 5 + 2�i0x00 �� 0xFF�j
                    modifiedBytes[0] = 0x53; // 'S'
                    modifiedBytes[1] = 0x54; // 'T'
                    modifiedBytes[2] = 0x41; // 'A'
                    modifiedBytes[3] = 0x52; // 'R'
                    modifiedBytes[4] = 0x54; // 'T'

                    // WAV�t�@�C���̃o�C�i���� modifiedBytes �� 5 �o�C�g�ڂ���ǉ�
                    Array.Copy(fileBytes, 0, modifiedBytes, 5, fileBytes.Length);

                    // �����ɒǉ��̃o�C�g��ݒ�
                    int startIndex = 5 + fileBytes.Length; // �ǉ��o�C�g�̊J�n�C���f�b�N�X
                    modifiedBytes[startIndex] = 0x45; // 'E'
                    modifiedBytes[startIndex + 1] = 0x4E; // 'N'
                    modifiedBytes[startIndex + 2] = 0x44; // 'D'
                    modifiedBytes[startIndex + 3] = 0x49; // 'I'
                    modifiedBytes[startIndex + 4] = 0x4E; // 'N'
                    modifiedBytes[startIndex + 5] = 0x47; // 'G'

                    // �ύX��̃o�C�i���f�[�^���ŏ���512�o�C�g��\��
                    // int displayLength2 = Math.Min(modifiedBytes.Length, 512); // �ŏ���512�o�C�g�����\��
                    // string modifiedHexString = BitConverter.ToString(modifiedBytes.Take(displayLength2).ToArray());

                    // �ύX��o�C�i���f�[�^�̍Ō��512�o�C�g��\��
                    // byte[] lastBytes2 = modifiedBytes.Skip(Math.Max(0, modifiedBytes.Length - 512)).ToArray();
                    // string lastHexString2 = BitConverter.ToString(lastBytes2);

                    // ���b�Z�[�W�{�b�N�X�ɕ\��
                    // MessageBox.Show($"�ύX���512�t�@�C����16�i���`��:\n{modifiedHexString}");
                    // MessageBox.Show($"�ύX���512�o�C�g��16�i���`��:\n{lastHexString2}");

                    // �t�@�C���ۑ��_�C�A���O��\��
                    using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                    {
                        saveFileDialog.Filter = "Text files (*.txt)|*.txt"; // �e�L�X�g�t�@�C���̂�
                        saveFileDialog.Title = "�o�C�i���f�[�^�̕ۑ�����w�肵�Ă�������";
                        saveFileDialog.FileName = "binary_output.txt"; // �f�t�H���g�̃t�@�C����

                        // ���[�U�[���ۑ�����w�肵���ꍇ�̂ݏ�����i�߂�
                        if (saveFileDialog.ShowDialog() == DialogResult.OK)
                        {
                            // �o�C�i���f�[�^���e�L�X�g�t�@�C���ɕۑ�
                            string hexData = BitConverter.ToString(modifiedBytes).Replace("-", " "); // �o�C�i���f�[�^��16�i���`���ɕϊ�
                            await File.WriteAllTextAsync(saveFileDialog.FileName, hexData); // �e�L�X�g�t�@�C���ɕۑ�
                            MessageBox.Show($"�o�C�i���f�[�^�� {saveFileDialog.FileName} �ɕۑ����܂����B");
                        }
                        else
                        {
                            MessageBox.Show("�t�@�C���ۑ����L�����Z������܂����B");
                        }
                    }

                    // CR�ʐM�ő��M
                    if (serialPort.IsOpen && modifiedBytes.Length > 0)
                    {
                        serialPort.Write(modifiedBytes, 0, modifiedBytes.Length);
                        MessageBox.Show("�f�[�^�𑗐M���܂����B");
                    }
                    else
                    {
                        MessageBox.Show("�V���A���|�[�g���J���Ă��܂���B");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"�G���[���������܂���: {ex.Message}");
                }
            }
        }

        //�@Local�{�^����`�悷��
        private async void Local_button11(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "WAV�t�@�C�� (*.wav)|*.wav"; // �t�B���^��ݒ�
                openFileDialog.Title = "WAV�t�@�C����I��";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string sendfilePath = openFileDialog.FileName; // �I�������t�@�C���̃p�X
                    string outputFilePath = "";

                    // �ۑ�������[�U�[�Ɏw�肳����
                    using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                    {
                        saveFileDialog.Filter = "WAV�t�@�C�� (*.wav)|*.wav";
                        saveFileDialog.Title = "�ۑ�����w��";
                        saveFileDialog.FileName = "converted_8000Hz.wav";

                        if (saveFileDialog.ShowDialog() == DialogResult.OK)
                        {
                            outputFilePath = saveFileDialog.FileName;

                            // �T���v�����O���[�g��8000Hz�ɕϊ�
                            ConvertWavFileSampleRate(sendfilePath, outputFilePath);
                        }
                    }

                    try
                    {
                        // �T���v�����O���[�g��8000Hz�ɕϊ������t�@�C����ǂݍ���
                        byte[] fileBytes = await Task.Run(() => File.ReadAllBytes(outputFilePath));

                        // �ǂݍ��񂾃o�C�i���f�[�^�̍ŏ���512�o�C�g��\��
                        // int displayLength = Math.Min(fileBytes.Length, 512); // �ŏ���512�o�C�g�����擾
                        // string firstHexString = BitConverter.ToString(fileBytes.Take(displayLength).ToArray());

                        // �ǂݍ��񂾃o�C�i���f�[�^�̍Ō��512�o�C�g��\��
                        // byte[] lastBytes = fileBytes.Skip(Math.Max(0, fileBytes.Length - 512)).ToArray();
                        // string lastHexString = BitConverter.ToString(lastBytes);

                        // ���b�Z�[�W�{�b�N�X�ɕ\��
                        // MessageBox.Show($"�ŏ���512�o�C�g��16�i���`��:\n{firstHexString}");
                        // MessageBox.Show($"�Ō��512�o�C�g��16�i���`��:\n{lastHexString}");

                        // �o�C�i���̐擪�Ɏw�肵���o�C�g��ǉ����邽�߂̔z����쐬
                        byte[] modifiedBytes = new byte[fileBytes.Length + 11]; // 5 + 2�i0x00 �� 0xFF�j
                        modifiedBytes[0] = 0x53; // 'S'
                        modifiedBytes[1] = 0x54; // 'T'
                        modifiedBytes[2] = 0x41; // 'A'
                        modifiedBytes[3] = 0x52; // 'R'
                        modifiedBytes[4] = 0x54; // 'T'

                        // WAV�t�@�C���̃o�C�i���� modifiedBytes �� 5 �o�C�g�ڂ���ǉ�
                        Array.Copy(fileBytes, 0, modifiedBytes, 5, fileBytes.Length);

                        // �����ɒǉ��̃o�C�g��ݒ�
                        int startIndex = 5 + fileBytes.Length; // �ǉ��o�C�g�̊J�n�C���f�b�N�X
                        modifiedBytes[startIndex] = 0x45; // 'E'
                        modifiedBytes[startIndex + 1] = 0x4E; // 'N'
                        modifiedBytes[startIndex + 2] = 0x44; // 'D'
                        modifiedBytes[startIndex + 3] = 0x49; // 'I'
                        modifiedBytes[startIndex + 4] = 0x4E; // 'N'
                        modifiedBytes[startIndex + 5] = 0x47; // 'G'

                        // �ύX��̃o�C�i���f�[�^���ŏ���512�o�C�g��\��
                        // int displayLength2 = Math.Min(modifiedBytes.Length, 512); // �ŏ���512�o�C�g�����\��
                        // string modifiedHexString = BitConverter.ToString(modifiedBytes.Take(displayLength2).ToArray());

                        // �ύX��o�C�i���f�[�^�̍Ō��512�o�C�g��\��
                        // byte[] lastBytes2 = modifiedBytes.Skip(Math.Max(0, modifiedBytes.Length - 512)).ToArray();
                        // string lastHexString2 = BitConverter.ToString(lastBytes2);

                        // ���b�Z�[�W�{�b�N�X�ɕ\��
                        // MessageBox.Show($"�ύX���512�t�@�C����16�i���`��:\n{modifiedHexString}");
                        // MessageBox.Show($"�ύX���512�o�C�g��16�i���`��:\n{lastHexString2}");

                        // �t�@�C���ۑ��_�C�A���O��\��
                        using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                        {
                            saveFileDialog.Filter = "Text files (*.txt)|*.txt"; // �e�L�X�g�t�@�C���̂�
                            saveFileDialog.Title = "�o�C�i���f�[�^�̕ۑ�����w�肵�Ă�������";
                            saveFileDialog.FileName = "binary_output.txt"; // �f�t�H���g�̃t�@�C����

                            // ���[�U�[���ۑ�����w�肵���ꍇ�̂ݏ�����i�߂�
                            if (saveFileDialog.ShowDialog() == DialogResult.OK)
                            {
                                // �o�C�i���f�[�^���e�L�X�g�t�@�C���ɕۑ�
                                string hexData = BitConverter.ToString(modifiedBytes).Replace("-", " "); // �o�C�i���f�[�^��16�i���`���ɕϊ�
                                await File.WriteAllTextAsync(saveFileDialog.FileName, hexData); // �e�L�X�g�t�@�C���ɕۑ�
                                MessageBox.Show($"�o�C�i���f�[�^�� {saveFileDialog.FileName} �ɕۑ����܂����B");
                            }
                            else
                            {
                                MessageBox.Show("�t�@�C���ۑ����L�����Z������܂����B");
                            }
                        }

                        // CR�ʐM�ő��M
                        if (serialPort.IsOpen && modifiedBytes.Length > 0)
                        {
                            serialPort.Write(modifiedBytes, 0, modifiedBytes.Length);
                            MessageBox.Show("�f�[�^�𑗐M���܂����B");
                        }
                        else
                        {
                            MessageBox.Show("�V���A���|�[�g���J���Ă��܂���B");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"�G���[���������܂���: {ex.Message}");
                    }
                }
            }
        }

        private async void ConvertWavFileSampleRate(string inputFilePath, string outputFilePath)
        {
            try
            {
                // ���̓t�@�C���̓ǂݍ���
                using (WaveFileReader reader = new WaveFileReader(inputFilePath))
                {
                    // �ϊ���̃t�H�[�}�b�g��8000Hz�A8�r�b�g�A���m�����ɐݒ�
                    WaveFormat newFormat = new WaveFormat(8000, 8, 1);

                    // �T���v�����O���[�g�̕ϊ�
                    using (WaveFormatConversionStream conversionStream = new WaveFormatConversionStream(newFormat, reader))
                    {
                        // �ϊ����WAV�t�@�C����ۑ�
                        WaveFileWriter.CreateWaveFile(outputFilePath, conversionStream);
                    }
                }

                // �ϊ��������������Ƃ����[�U�[�ɒʒm
                MessageBox.Show($"�t�@�C���̃T���v�����O���[�g��8000Hz�ɕϊ����܂���: {outputFilePath}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"�G���[���������܂���: {ex.Message}");
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // �V���A���|�[�g�����
            if (serialPort.IsOpen)
            {
                serialPort.Close();
            }
            base.OnFormClosed(e);
        }

        // TTS�֘A�@//
        private string filename;
        private int fileindex;

        // Synthesize�{�^����`�悷��
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
            string speakerid = Getspeechsynthesischaracter(comboBox1.Text); // �b��ID���擾
            filename += "_" + comboBox1.Text;
            string audioQuery = await GetAudioQuery(text, speakerid);
            if (audioQuery != null)
            {
                await SynthesizeSpeech(audioQuery, speakerid);
            }

            // �����t�@�C�����^�C�����C���ɒǉ�
            AddToTimeline(filename + ".wav");

            fileindex++;
            button8.Enabled = true;
        }

        // �������� VOICEVOX �p�֐� 
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
                {"�l���߂���","2"},
                {"���񂾂���","3"},
                {"�t�����ނ�","8"},
                {"�J���͂�","10"},
                {"�g�����c","9"},
                {"���앐�G","11"},
                {"����Ց��Y","12"},
                {"�R����","13"},
                {"���Ђ܂�","14"},
                {"��B����","16"},
                {"�����q����","20"},
                {"���莓�Y","21"},
                {"WhiteCUL","23"},
                {"��S","27"},
                {"No.7","29"},
                {"���ю�����","42"},
                {"�N�̃~�R","43"},
                {"����/SAYO","46"},
                {"�i�[�X���{�Q�^�C�v�s","47"},
                {"�����R�m �g����","51"},
                {"������i","52"},
                {"�i�����@��","53"},
                {"�t�̃i�i","54"},
                {"�L�g�A��","55"},
                {"�L�g�r�B","58"},
                {"����������","61"},
                {"�I�c�܂��","67"},
                {"�������邽��","68"},
                {"���ʉԊ�","69"},
                {"�Չr�j�A","74"}
            };

            // speakername����܂���null�̏ꍇ�̃`�F�b�N
            if (string.IsNullOrEmpty(speakername))
            {
                MessageBox.Show("�b�Җ����I������Ă��܂���B");
                return string.Empty; // �܂��̓f�t�H���g�̘b��ID��Ԃ�
            }

            // �b�Җ��������ɑ��݂��邩���m�F
            if (speakerids.ContainsKey(speakername))
            {
                return speakerids[speakername];
            }
            else
            {
                MessageBox.Show("�w�肳�ꂽ�b�Җ���������܂���: " + speakername);
                return string.Empty; // �܂��̓f�t�H���g�̘b��ID��Ԃ�
            }
        }

        private async Task<string> GetAudioQuery(string text, string speakerid)
        {
            var response = await client.PostAsync($"{VOICEVOXurl}/audio_query?text={text}&speaker={speakerid}", null);
            if (response.IsSuccessStatusCode)
            {
                var query = await response.Content.ReadAsStringAsync();

                // JSON�f�[�^���I�u�W�F�N�g�ɕϊ�
                dynamic queryJson = Newtonsoft.Json.JsonConvert.DeserializeObject(query);

                // �p�����[�^�ݒ�
                queryJson.speedScale = Convert.ToDouble(textBox2.Text);
                queryJson.pitchScale = Convert.ToDouble(textBox3.Text);
                queryJson.intonationScale = Convert.ToDouble(textBox4.Text);

                // �I�u�W�F�N�g��JSON�f�[�^�ɍĕϊ�
                return Newtonsoft.Json.JsonConvert.SerializeObject(queryJson);
            }
            return null;
        }

        // SynthesizeSpeech ���\�b�h�ŉ����t�@�C����ۑ�������AAddToTimeline ���\�b�h���Ăяo��
        private async Task SynthesizeSpeech(string audioQuery, string speakerid)
        {
            var content = new StringContent(audioQuery, Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{VOICEVOXurl}/synthesis?speaker={speakerid}", content);
            if (response.IsSuccessStatusCode)
            {
                byte[] audioData = await response.Content.ReadAsByteArrayAsync();

                // �����f�[�^��ۑ�
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

        //�@TextEntry�{�b�N�X��`�悷��
        private void TextEntry_textBox1(object sender, EventArgs e)
        {

        }

        //�@TextEntry�{�b�N�X��`�悷��
        private void TTSCharacter_comboBox1(object sender, EventArgs e)
        {

        }

        //�@ReadingSpeed�{�b�N�X��`�悷��
        private void ReadingSpeed_textBox2(object sender, EventArgs e)
        {

        }

        //�@ReadingSpeed�o�[��`�悷��
        private void ReadingSpeed_trackBar1(object sender, EventArgs e)
        {
            textBox2.Text = Convert.ToString(trackBar1.Value / 4f);   // �ǂݏグ���x
        }

        //�@VoiceHeight�{�b�N�X��`�悷��
        private void VoiceHeight_textBox3(object sender, EventArgs e)
        {

        }

        //�@VoiceHeight�o�[��`�悷��
        private void VoiceHeight_trackBar2(object sender, EventArgs e)
        {
            textBox3.Text = Convert.ToString(trackBar2.Value / 20f);  // ���̍���
        }

        //�@Intonation�{�b�N�X��`�悷��
        private void textBox4_TextChanged(object sender, EventArgs e)
        {

        }

        //�@Intonation�o�[��`�悷��
        private void Intonation_trackBar3(object sender, EventArgs e)
        {
            textBox4.Text = Convert.ToString(trackBar3.Value * 0.2f); // �}�g
        }

        //�@TTS�O���[�v�{�b�N�X��`�悷��
        private void TTS_groupBox1(object sender, EventArgs e)
        {

        }

        // VC�֘A�@//
        private string Getvoicechangercharacter(string speakername)
        {
            Dictionary<string, string> speakerids = new Dictionary<string, string>()
            {
                {"���񂾂���","zundamon-1"},
                {"���݂���","Amitaro_Zero_100e_3700s"}
            };
            return speakerids[speakername];
        }

        private bool recording = false;
        WaveInEvent VCwaveIn;
        WaveFileWriter VCwaveWriter;

        //�@
        private void Recording_button9(object sender, EventArgs e)
        {
            if (!recording)
            {
                var deviceNumber = 0;

                // �^�揈�����J�n
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

                // �{�C�X�`�F���W�J�n
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

                // �t�@�C�����^�C�����C���ɒǉ�
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

        //�@VC�O���[�v�{�b�N�X��`�悷��
        private void VC_groupBox2(object sender, EventArgs e)
        {

        }

        //�@Playback�o�[��`�悷��
        private void PlaybackBar_panel2(object sender, PaintEventArgs e)
        {

        }

        //�@Playback�o�[�̈ʒu���X�V����
        private void UpdatePlaybackBar(TimeSpan currentTime, TimeSpan totalTime)
        {
            // �^�C�����C���̑S�̂̕����s�N�Z���P�ʂŎ擾
            int timelineWidth = panel1.Width; // `panel1` �̓^�C�����C���̕\���̈�

            // ���݂̍Đ��ʒu�Ɋ�Â��ăo�[�̕����v�Z
            double percentage = currentTime.TotalMilliseconds / totalTime.TotalMilliseconds;
            int newWidth = (int)(timelineWidth * percentage);

            // �Đ��o�[�̈ʒu���X�V
            panel2.Width = newWidth;
        }

        //�@���݂̍Đ��ʒu��\������
        private void PlaybackTime_label3(object sender, EventArgs e)
        {

        }

        //�@�Đ����̎��Ԃ����A���^�C���ōX�V���A�Đ����I���������`�F�b�N����
        private void PlaybackTimer_Tick(object sender, EventArgs e)
        {
            var currentTime = _audioPlayer.CurrentTime;

            // �^�C�����C�����I���n�_�ɒB���������m�F
            if (currentTime >= _audioPlayer.TotalTime)
            {
                // �Đ����~
                StopPlayback();
            }
            else
            {
                // TrackBar �ƃ��x�����X�V
                UpdateTrackBar(currentTime, _audioPlayer.TotalTime);
                label3.Text = $"Playback Time: {currentTime.ToString(@"hh\:mm\:ss")}";

                // �Đ��o�[���X�V
                UpdatePlaybackBar(currentTime, _audioPlayer.TotalTime);
            }
        }

        //�@�I�[�f�B�I�̍Đ����~����ۂɁA�Đ��̏I��������UI�̍X�V������
        private void StopPlayback()
        {
            // �^�C�}�[���~
            _playbackTimer.Stop();

            // �����Đ����~
            _audioPlayer.Stop();

            // TrackBar ���I���n�_�ɃZ�b�g
            UpdateTrackBar(_audioPlayer.TotalTime, _audioPlayer.TotalTime);
        }

        //�@PlaybackBar_panel2��PlaybackTime_label3���X�V������
        private void UpdateTrackBar(TimeSpan currentTime, TimeSpan totalTime)
        {
            trackBarTime.Minimum = 0;
            trackBarTime.Maximum = (int)totalTime.TotalMilliseconds;
            trackBarTime.Value = (int)currentTime.TotalMilliseconds;
            labelTime.Text = $"Time: {currentTime.ToString(@"hh\:mm\:ss")}";
        }

        //�@�ł������Đ��J�n���Ԃ�\������
        private void StartTime_label2(object sender, EventArgs e)
        {

        }

        // �^�C�����C���̊J�n���Ԃ�\��
        private void DisplayFirstObjectStartTime()
        {
            // �^�C�����C����̑S�I�u�W�F�N�g���擾
            List<TimelineObject> timelineObjects = _timeline.GetObjects();

            // �ŏ��̃I�u�W�F�N�g�̊J�n���Ԃ��擾
            TimeSpan firstStartTime = GetFirstObjectStartTime(timelineObjects);

            // �J�n���Ԃ����x���ɕ\��
            label2.Text = $"Start Time: {firstStartTime.ToString(@"hh\:mm\:ss")}";
        }

        // �^�C�����C���̊J�n���Ԃ��擾
        private TimeSpan GetFirstObjectStartTime(List<TimelineObject> timelineObjects)
        {
            if (timelineObjects == null || timelineObjects.Count == 0)
            {
                return TimeSpan.Zero; // �I�u�W�F�N�g���Ȃ��ꍇ��0��Ԃ�
            }

            // �ŏ��̃I�u�W�F�N�g�̊J�n���Ԃ��擾
            TimeSpan firstStartTime = timelineObjects[0].StartTime;

            // �S�I�u�W�F�N�g�̊J�n���Ԃ��m�F���A�ł��������̂�I��
            foreach (var obj in timelineObjects)
            {
                if (obj.StartTime < firstStartTime)
                {
                    firstStartTime = obj.StartTime;
                }
            }

            return firstStartTime;
        }

        //�@�ł��x���Đ��I�����Ԃ�\������
        private void EndTime_label1(object sender, EventArgs e)
        {

        }

        // �^�C�����C���̏I�����Ԃ�\��
        private void UpdateEndTimeLabel(TimeSpan endTime)
        {
            label1.Text = $"End Time: {endTime.ToString(@"hh\:mm\:ss")}";
        }

        // �I�����Ԃ��X�V
        private void UpdateTimelineEndTime()
        {
            TimeSpan endTime = GetMaximumEndTime();
            UpdateEndTimeLabel(endTime);
        }

        // �^�C�����C���I�u�W�F�N�g�̏I�����Ԃ��r
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

        //�@timeline�̉��X�N���[���o�[��`�悷��
        private void Timeline_hScrollBar1(object sender, ScrollEventArgs e)
        {
            // �X�N���[���o�[�̒l��Minimum��Maximum�͈͓̔��ł��邩���m�F
            if (e.NewValue >= hScrollBar1.Minimum && e.NewValue <= hScrollBar1.Maximum)
            {
                hScrollBar1.Value = e.NewValue;
                // �X�N���[���ʒu�Ɋ�Â��ă^�C�����C�����ĕ`��
                panel1.Invalidate(); // ����Ńp�l���̍ĕ`�悪�g���K�[����܂�
            }
        }

        // �t�H�[���̏��������ɃX�N���[���o�[�̐ݒ�
        private void hScrollBar1_Load(object sender, EventArgs e)
        {
            // �X�N���[���o�[�̐ݒ�
            hScrollBar1.Minimum = 0;
            hScrollBar1.Maximum = CalculateMaxScroll(); // �^�C�����C���̍ő啝�ɉ����Đݒ�
            hScrollBar1.LargeChange = ClientSize.Width; // �\���̈�̕�
            hScrollBar1.SmallChange = 10; // �X�N���[�����̏����ȕω���
            hScrollBar1.Value = 0; // �����l
        }

        // �^�C�����C���̍ő�X�N���[�������v�Z���郁�\�b�h
        private int CalculateMaxScroll()
        {
            // �^�C�����C����̍ł��E���̃I�u�W�F�N�g�̈ʒu���v�Z
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
            return maxRight - ClientSize.Width; // �X�N���[���̍ő啝
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }
    }
    
    //�@�^�C�����C����̃I�[�f�B�I�I�u�W�F�N�g���Ǘ�����
    public class Timeline
    {
        private List<TimelineObject> _objects = new List<TimelineObject>();

        //�@�V�����I�u�W�F�N�g���^�C�����C���ɒǉ�����
        public void AddObject(TimelineObject newObject)
        {
            int targetLayer = 0;
            bool overlapFound;

            do
            {
                overlapFound = false;

                // �������C���[���̃I�u�W�F�N�g�Ƃ̏Փ˂��`�F�b�N
                foreach (var existingObject in _objects)
                {
                    if (existingObject.Layer == targetLayer && IsOverlapping(existingObject, newObject))
                    {
                        overlapFound = true;
                        targetLayer++;  // ���C���[��1��Ɉړ�
                        break;
                    }
                }
            } while (overlapFound);

            // �Փ˂̂Ȃ����C���[�����������̂ŁA���̃��C���[�ɔz�u
            newObject.Layer = targetLayer;
            _objects.Add(newObject);
        }

        // �I�u�W�F�N�g���d�Ȃ��Ă��邩�ǂ����𔻒肷��
        private bool IsOverlapping(TimelineObject obj1, TimelineObject obj2)
        {
            return obj1.StartTime < obj2.StartTime + obj2.Duration &&
                   obj2.StartTime < obj1.StartTime + obj1.Duration;
        }

        //�@�w�肵���I�u�W�F�N�g���^�C�����C������폜����
        public void RemoveObject(TimelineObject obj)
        {
            _objects.Remove(obj);
        }

        //�@�^�C�����C����̂��ׂẴI�u�W�F�N�g�̃��X�g��Ԃ�
        public List<TimelineObject> GetObjects()
        {
            return _objects;
        }

        //�@�^�C�����C���S�̂̒������擾����
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

        // IsSelected = true�̃I�u�W�F�N�g���擾����
        public List<TimelineObject> GetSelectedObjects()
        {
            return _objects.Where(o => o.IsSelected).ToList();
        }
    }

    // �I�u�W�F�N�g�̃v���p�e�B��ێ�����
    public class TimelineObject
    {
        private TimeSpan _startTime;
        private WaveOffsetStream _waveOffsetStream; // WaveOffsetStream���g�p����
        public TimeSpan Duration { get; set; }
        public TimeSpan EndTime => StartTime + Duration;
        public int Layer { get; set; }
        public string FilePath { get; set; }
        public bool IsSelected { get; set; } // �I�����
        public Point DrawingPosition { get; set; }�@// �`����W��ێ�����

        public string FileName => Path.GetFileName(FilePath);

        //�@������
        public TimelineObject(TimeSpan startTime, WaveOffsetStream waveOffsetStream, TimeSpan duration, int layer, string filePath)
        {
            _startTime = startTime;
            _waveOffsetStream = waveOffsetStream;
            Duration = duration;
            Layer = layer;
            FilePath = filePath;
            IsSelected = false; // ������Ԃł͑I������Ă��Ȃ�
        }

        // StartTime�v���p�e�B
        public TimeSpan StartTime
        {
            get => _startTime;
            set
            {
                _startTime = value;
                if (_waveOffsetStream != null)
                {
                    // StartTime�̕ύX��WaveOffsetStream�ɔ��f
                    _waveOffsetStream.StartTime = _startTime;
                }
            }
        }
    }

    // �Đ��֘A�̋@�\���
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

        // ������
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

        //�@�w�肳�ꂽ�t�@�C���p�X����I�[�f�B�I�t�@�C����ǂݍ���
        public TimelineObject Load(string filePath)
        {
            var wavePlayer = new CustomWavePlayer(); // �����Ȃ��̃R���X�g���N�^�[���g�p
            var waveStream = new AudioFileReader(filePath);

            // PCM�ϊ������FMediaFoundationReader�œǂݍ��݁APCM�t�H�[�}�b�g�ɕϊ�
            var reader = new MediaFoundationReader(filePath);
            var pcmStream = WaveFormatConversionStream.CreatePcmStream(reader);

            // WaveOffsetStream�̍쐬
            var waveOffsetStream = new CustomWaveOffsetStream(pcmStream, TimeSpan.Zero, TimeSpan.Zero, pcmStream.TotalTime);

            wavePlayer.Init(waveOffsetStream); // CustomWavePlayer��������
            _wavePlayers.Add(wavePlayer);
            _waveOffsetStreams.Add(waveOffsetStream); // �I�t�Z�b�g�X�g���[�������X�g�ɒǉ�

            // waveOffsetStream �� CustomWaveStream �ɕϊ�
            var customWaveStream = new CustomWaveStream(waveOffsetStream);

            _waveStreams.Add(customWaveStream); // waveStream �����X�g�ɒǉ�
            _filePathWaveStreamMap[filePath] = customWaveStream; // waveStream ���}�b�v�ɒǉ�
            _waveStreamPlayerMap[customWaveStream] = wavePlayer;
            _filePathWaveOffsetStreamMap[filePath] = waveOffsetStream; // offsetStream ���}�b�v�ɒǉ�

            // Duration�́AwaveStream����擾����
            TimeSpan duration = customWaveStream.TotalTime;

            // TimelineObject���쐬���ď����i�[
            var timelineObject = new TimelineObject(
                startTime: TimeSpan.Zero,
                waveOffsetStream: waveOffsetStream,
                duration: duration,
                layer: 0,
                filePath: filePath
            )
            {
                IsSelected = false // ������Ԃł͑I������Ă��Ȃ����̂Ƃ���
            };

            return timelineObject;
        }

        // ���ׂẴI�[�f�B�I�v���C���[�ōĐ�����
        public void Play()
        {
            foreach (var player in _wavePlayers)
            {
                player.Play();
            }
        }

        //�@���ׂẴI�[�f�B�I�v���C���[�ōĐ����~����
        public void Stop()
        {
            foreach (var player in _wavePlayers)
            {
                player.Stop();
            }
        }

        //�@���ׂẴI�[�f�B�I�X�g���[���̍Đ��ʒu��擪�ɖ߂�
        public void Reset()
        {
            foreach (var stream in _waveStreams)
            {
                stream.Position = 0; // �Đ��ʒu��擪�ɖ߂�
            }
        }

        // ���ׂẴI�u�W�F�N�g���폜����
        public void Clean()
        {
            // ���\�[�X��������Ă��烊�X�g���N���A����
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

        // �I�����ꂽ�I�u�W�F�N�g�Ɋ֘A���� WaveStream ���폜����
        public void Delete(List<TimelineObject> selectedObjects)
        {
            foreach (var obj in selectedObjects)
            {
                // TimelineObject �ɕR�t�����Ă���t�@�C���p�X���� WaveStream ���擾
                var filePath = obj.FilePath;

                // �t�@�C���p�X�Ɋ�Â��� WaveStream �����X�g���猟��
                if (_filePathWaveStreamMap.TryGetValue(filePath, out var waveStream) && waveStream is CustomWaveStream customWaveStream)
                {
                    // wavePlayer �̎擾
                    if (_waveStreamPlayerMap.TryGetValue(waveStream, out var wavePlayer) && wavePlayer is CustomWavePlayer customWavePlayer)
                    {
                        customWavePlayer.Stop(); // �Đ����~
                        customWavePlayer.Dispose(); // WavePlayer �����
                        _wavePlayers.Remove(customWavePlayer); // _wavePlayers ���X�g����폜
                        _waveStreamPlayerMap.Remove(waveStream); // �}�b�v����폜

                        // WaveStream ��������A���X�g����폜
                        customWaveStream.Dispose();
                        _waveStreams.Remove(customWaveStream); // _waveStreams ���X�g����폜
                        _filePathWaveStreamMap.Remove(filePath); // _filePathWaveStreamMap ����폜
                    }
                }
            }
        }

        // ���݂̍Đ��ʒu���擾����
        public TimeSpan CurrentTime
        {
            get
            {
                if (_waveStreams.Count > 0)
                    return _waveStreams[0].CurrentTime;
                return TimeSpan.Zero;
            }
        }

        // �I�[�f�B�I�t�@�C���̑��Đ����Ԃ��擾����
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

    //�@WaveOffsetStream �̃��b�p�[�N���X
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
            // WaveStream �� CustomWaveStream �ɕϊ�
            return new CustomWaveStream(this); // ���̃N���X�� WaveStream ���p�����Ă��邽�߁A���ڎg�p�\
        }

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    // �}�l�[�W���\�[�X�̉��
                    base.Dispose(disposing);
                }

                // ��}�l�[�W���\�[�X�̉��������΂����ɋL�q

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

    //�@WaveStream �̃��b�p�[�N���X
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

        // Dispose���\�b�h�ȂǑ��̕K�v�ȃ��\�b�h������
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _sourceStream?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    //�@IWavePlayer �̃��b�p�[�N���X
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
            get => _waveOut.Volume; // WaveOutEvent��Volume��Ԃ�
            set => _waveOut.Volume = value; // WaveOutEvent��Volume��ݒ�
        }

        // �����Ȃ��̃R���X�g���N�^
        public CustomWavePlayer()
        {
            _waveOut = new WaveOutEvent();
            IsDisposed = false;
        }

        // ��������̃R���X�g���N�^
        public CustomWavePlayer(IWavePlayer player)
        {
            Player = player;
            _waveOut = new WaveOutEvent(); // WaveOutEvent�̏�����
            IsDisposed = false;
        }

        public void Init(IWaveProvider waveProvider)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(CustomWavePlayer)); // Dispose����Ă���ꍇ�͗�O���X���[

            _waveOut.Init(waveProvider);
        }

        public void Play()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(CustomWavePlayer)); // Dispose����Ă���ꍇ�͗�O���X���[

            _waveOut.Play();
        }

        public void Pause()
        {
            _waveOut.Pause();
        }

        public void Stop()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(CustomWavePlayer)); // Dispose����Ă���ꍇ�͗�O���X���[

            _waveOut.Stop(); // WaveOutEvent��Stop���\�b�h���Ăяo��
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
