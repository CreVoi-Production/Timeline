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
        private TimeSpan _currentPlaybackTime; //�@���݂̍Đ����Ԃ�ێ�����
        private bool _isPlaying; //�@�Đ���Ԃ�����
        private TimelineObject _selectedObject; //�@�I������Ă���I�u�W�F�N�g�̕ێ�����
        private Point _dragStartPoint; //�@�h���b�O�̊J�n�ʒu��ێ�����
        private bool _isDragging; //�@�h���b�O�����Ԃ�����
        private int numberOfLayers = 100; // ���C���[���̏����l
        private int layerHeight = 50; // �e���C���[�̍���
        private double pixelsPerMillisecond => (double)panel1.ClientSize.Width / _timeline.TotalDuration.TotalMilliseconds; // 1�~���b������̃s�N�Z�����i���Ԏ��̃X�P�[���j�@

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
                TimelineObject timelineObject = _audioPlayer.Load(filePath);

                // �^�C�����C���I�u�W�F�N�g�Ƃ��Ēǉ�
                _timeline.AddObject(timelineObject);

                // �^�C�����C���̒����������t�@�C���̒����ɍ��킹�čX�V
                UpdateTrackBar(TimeSpan.Zero, _audioPlayer.TotalTime);

                // �ł������J�n���Ԃ�\��
                DisplayFirstObjectStartTime();

                // �^�C�����C���̏I�����Ԃ��X�V
                UpdateTimelineEndTime();

                // �^�C�����C�����ĕ`��
                panel1.Invalidate();
            }
        }

        //�@Clean�{�^����`�悷��
        private void Clean_button5(object sender, EventArgs e)
        {
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

        //�@Export�{�^����`�悷��
        private void Export_button6(object sender, EventArgs e)
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
            public TimeSpan StartTime { get; set; }
            public TimeSpan Duration { get; set; }
            public TimeSpan EndTime => StartTime + Duration;
            public int Layer { get; set; }
            public string FilePath { get; set; }
            public bool IsSelected { get; set; } // �I�����
            public Point DrawingPosition { get; set; }�@// �`����W��ێ�����

            public string FileName => Path.GetFileName(FilePath);

            //�@������
            public TimelineObject(TimeSpan startTime, TimeSpan duration, int layer, string filePath)
            {
                StartTime = startTime;
                Duration = duration;
                Layer = layer;
                FilePath = filePath;
                IsSelected = false; // ������Ԃł͑I������Ă��Ȃ�
            }
        }

        // �Đ��֘A�̋@�\���
        public class AudioPlayer
        {
            private Timeline _timeline;

            private List<IWavePlayer> _wavePlayers;
            private List<WaveStream> _waveStreams;
            private Dictionary<WaveStream, IWavePlayer> _waveStreamPlayerMap;
            private Dictionary<string, WaveStream> _filePathWaveStreamMap;

            public WaveStream waveStream { get; set; }
            public IWavePlayer wavePlayer { get; set; }

            // ������
            public AudioPlayer()
            {
                _timeline = new Timeline();

                _wavePlayers = new List<IWavePlayer>();
                _waveStreams = new List<WaveStream>();
                _filePathWaveStreamMap = new Dictionary<string, WaveStream>();
                _waveStreamPlayerMap = new Dictionary<WaveStream, IWavePlayer>();
            }

            //�@�w�肳�ꂽ�t�@�C���p�X����I�[�f�B�I�t�@�C����ǂݍ���
            public TimelineObject Load(string filePath)
            {
                var wavePlayer = new WaveOutEvent();
                var waveStream = new AudioFileReader(filePath);

                wavePlayer.Init(waveStream);
                _wavePlayers.Add(wavePlayer);
                _waveStreams.Add(waveStream);
                _filePathWaveStreamMap[filePath] = waveStream;
                _waveStreamPlayerMap[waveStream] = wavePlayer;

                // Duration�́AwaveStream����擾����
                TimeSpan duration = waveStream.TotalTime;

                // TimelineObject���쐬���ď����i�[
                var TimelineObject = new TimelineObject(
                    startTime: CurrentTime,
                    duration: duration,
                    layer: 0,
                    filePath: filePath
                )
                {
                    IsSelected = false // ������Ԃł͑I������Ă��Ȃ����̂Ƃ���
                };
                return TimelineObject;
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

                _wavePlayers.Clear();
                _waveStreams.Clear();
            }

            // �I�����ꂽ�I�u�W�F�N�g�Ɋ֘A���� WaveStream ���폜����
            public void Delete(List<TimelineObject> selectedObjects)
            {
                foreach (var obj in selectedObjects)
                {
                    // TimelineObject �ɕR�t�����Ă���t�@�C���p�X���� WaveStream ���擾
                    var filePath = obj.FilePath;

                    // �t�@�C���p�X�Ɋ�Â��� WaveStream �����X�g���猟��
                    _filePathWaveStreamMap.TryGetValue(filePath, out var waveStream);
                    _waveStreamPlayerMap.TryGetValue(waveStream, out var wavePlayer);

                    wavePlayer.Stop(); // �Đ����~
                    wavePlayer.Dispose(); // WavePlayer �����
                    _wavePlayers.Remove(wavePlayer); // _wavePlayers ���X�g����폜
                    _waveStreamPlayerMap.Remove(waveStream); // �}�b�v����폜

                     // WaveStream ��������A���X�g����폜
                    waveStream.Dispose();
                    _waveStreams.Remove(waveStream); // _waveStreams ���X�g����폜
                    _filePathWaveStreamMap.Remove(filePath); // _filePathWaveStreamMap ����폜
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
    }
}
