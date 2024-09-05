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
            _playbackTimer.Interval = 100; // �~���b�P�ʁA�����ł�100ms���ƂɍX�V
            _playbackTimer.Tick += PlaybackTimer_Tick;

            // TrackBar �̏����ݒ�
            UpdateTrackBar(TimeSpan.Zero, TimeSpan.FromSeconds(10)); // 10�b�̃^�C�����C��

            _audioPlayer = new AudioPlayer();

            // �h���b�O���h���b�v�̐ݒ�
            this.AllowDrop = true;
            this.DragEnter += Form1_DragEnter;
            this.DragDrop += Form1_DragDrop;

            //�I�u�W�F�N�g�̃h���b�O�ړ�
            panel1.MouseDown += TimelinePanel_MouseDown;
            panel1.MouseMove += TimelinePanel_MouseMove;
            panel1.MouseUp += TimelinePanel_MouseUp;
        }

        // �^�C�����C���̊J�n���Ԃ�\��
        private void DisplayFirstObjectStartTime()
        {
            // �^�C�����C����̑S�I�u�W�F�N�g���擾
            List<TimelineObject> timelineObjects = _timeline.GetObjects(); // ���̃��\�b�h�̓^�C�����C���̃I�u�W�F�N�g���X�g���擾������̂Ɖ���

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

        //�@�h���b�O���h���b�v�̎���
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

        //�@�h���b�O���h���b�v�̎���
        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            var filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (var filePath in filePaths)
            {
                // �����t�@�C���̃��[�h
                _audioPlayer.Load(filePath);

                // �����t�@�C�������������[�h����Ă��邩�m�F
                if (_audioPlayer.TotalTime != TimeSpan.Zero)
                {
                    // �^�C�����C���I�u�W�F�N�g�Ƃ��Ēǉ�
                    var timelineObject = LoadAudioFile(filePath);
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

        //�I�u�W�F�N�g�̃h���b�O�ړ�
        private void TimelinePanel_MouseDown(object sender, MouseEventArgs e)
        {
            // �N���b�N���ꂽ�ʒu�ɃI�u�W�F�N�g�����邩�m�F
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

        //�I�u�W�F�N�g�̃h���b�O�ړ�
        private void TimelinePanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && _selectedObject != null)
            {
                // �}�E�X�̈ړ��ʂ��v�Z
                int deltaX = e.X - _dragStartPoint.X;
                int deltaY = e.Y - _dragStartPoint.Y;

                // 1�s�N�Z��������̎��ԒP�ʂ�ݒ�i��: 1�~���b������1�s�N�Z���j
                double pixelsPerMillisecond = 1;

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

                // �ł������J�n���Ԃ�\��
                DisplayFirstObjectStartTime();

                // �^�C�����C���̏I�����Ԃ��X�V
                UpdateTimelineEndTime();

                // �^�C�����C�����ĕ`��
                panel1.Invalidate();
            }
        }

        //�I�u�W�F�N�g�̃h���b�O�ړ�
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

        // �I�u�W�F�N�g�̏d�Ȃ蔻��
        private bool IsOverlapping(TimelineObject obj1, TimelineObject obj2)
        {
            // obj1�̊J�n���Ԃ�obj2�̏I�����Ԃ��O�ŁA����obj1�̏I�����Ԃ�obj2�̊J�n���Ԃ���̏ꍇ�A�d�Ȃ��Ă���
            return obj1.StartTime < obj2.StartTime + obj2.Duration &&
                   obj1.StartTime + obj1.Duration > obj2.StartTime;
        }

        //�Đ��o�[�̈ʒu���X�V
        private void UpdatePlaybackBar(TimeSpan currentTime, TimeSpan totalTime)
        {
            // �^�C�����C���̑S�̂̕����s�N�Z���P�ʂŎ擾
            int timelineWidth = panel1.Width; // `panel1` �̓^�C�����C���̕\���̈�

            // ���݂̍Đ��ʒu�Ɋ�Â��ăo�[�̕����v�Z
            double percentage = currentTime.TotalMilliseconds / totalTime.TotalMilliseconds;
            int newWidth = (int)(timelineWidth * percentage);

            // �Đ��o�[�̈ʒu���X�V
            panelPlaybackBar.Width = newWidth;
        }

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
                labelPlaybackTime.Text = $"Playback Time: {currentTime.ToString(@"hh\:mm\:ss")}";

                // �Đ��o�[���X�V
                UpdatePlaybackBar(currentTime, _audioPlayer.TotalTime);
            }
        }

        private void StopPlayback()
        {
            // �^�C�}�[���~
            _playbackTimer.Stop();

            // �����Đ����~
            _audioPlayer.Stop();

            // TrackBar ���I���n�_�ɃZ�b�g
            UpdateTrackBar(_audioPlayer.TotalTime, _audioPlayer.TotalTime);
        }

        private void TimelinePanel_Paint(object sender, PaintEventArgs e)
        {
            // �^�C�����C���I�u�W�F�N�g��`�悷�鏈��
            foreach (var obj in _timeline.GetObjects())
            {
                DrawTimelineObject(e.Graphics, obj);
            }
        }

        private void DrawTimelineObject(Graphics g, TimelineObject obj)
        {
            // �I�u�W�F�N�g�̈ʒu�ƃT�C�Y���v�Z���ĕ`��
            int pixelsPerMillisecond = 1; // 1�~���b������̃s�N�Z�����i���Ԏ��̃X�P�[���j
            int layerHeight = 50; // �e���C���[�̍���

            int x = (int)(obj.StartTime.TotalMilliseconds * pixelsPerMillisecond);  // �~���b���s�N�Z���ɕϊ�
            int width = (int)(obj.Duration.TotalMilliseconds * pixelsPerMillisecond);  // �~���b���s�N�Z���ɕϊ�
            int y = obj.Layer * layerHeight;  // ���C���[���Ƃ�Y���W��ς���
            int height = layerHeight - 10; // �I�u�W�F�N�g�̍�����ݒ�

            // �I�u�W�F�N�g�̎l�p�`��`��
            g.FillRectangle(Brushes.Blue, x, y, width, height);
            g.DrawRectangle(Pens.Black, x, y, width, height);

            // �I�u�W�F�N�g���⑼�̏���`�悵�����ꍇ
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

                // �^�C�����C���I�u�W�F�N�g�Ƃ��Ēǉ�
                var timelineObject = LoadAudioFile(filePath);
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

        private TimelineObject LoadAudioFile(string filePath)
        {
            // �����t�@�C���̒������擾
            TimeSpan duration;
            using (var reader = new AudioFileReader(filePath))
            {
                duration = reader.TotalTime;
            }

            // �����ł́A�J�n���Ԃ�0�A���C���[��0�ɐݒ�
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

        // �I�u�W�F�N�g���d�Ȃ��Ă��邩�ǂ����𔻒肷�郁�\�b�h
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
                stream.Position = 0; // �Đ��ʒu��擪�ɖ߂�
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
