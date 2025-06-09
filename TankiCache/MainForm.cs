using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using NAudio.Wave;
using System.Runtime.InteropServices;
using Shell32;
using SHDocVw;

namespace ImageViewerWithBase64Names
{
    public partial class MainForm : Form
    {
        private string currentDirectory;
        private WaveOutEvent waveOut;
        private AudioFileReader audioFileReader;
        private bool isPlaying = false;
        private float lastVolume = 0.5f; // Default volume (50%)

        public MainForm()
        {
            InitializeComponent();
            fileListBox.SelectedIndexChanged += FileListBox_SelectedIndexChanged;
            volumeTrackBar.Value = (int)(lastVolume * 100); // Initialize slider
        }

        private void SelectFolderButton_Click(object sender, EventArgs e)
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    currentDirectory = folderDialog.SelectedPath;
                    LoadFilesFromDirectory();
                }
            }
        }

        private void LoadFilesFromDirectory()
        {
            fileListBox.Items.Clear();

            if (string.IsNullOrEmpty(currentDirectory) || !Directory.Exists(currentDirectory))
                return;

            try
            {
                var files = Directory.GetFiles(currentDirectory)
                    .Select(f => new
                    {
                        Path = f,
                        DecodedName = TryDecodeFileName(Path.GetFileNameWithoutExtension(f)),
                        IsImage = IsLikelyImageFile(f),
                        IsAudio = IsLikelyAudioFile(f),
                        IsXml = IsLikelyXmlFile(f)
                    })
                    .Where(f => f.IsImage || f.IsAudio || f.IsXml)
                    .OrderBy(f => f.DecodedName)
                    .ToArray();

                fileListBox.Items.Clear();
                fileListBox.DisplayMember = "DecodedName";
                fileListBox.ValueMember = "Path";
                fileListBox.Items.AddRange(files);

                if (files.Length == 0)
                {
                    MessageBox.Show("Не найдено подходящих файлов (изображения, аудио, XML) в выбранной директории.", "Информация",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке файлов: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string TryDecodeFileName(string fileName)
        {
            try
            {
                byte[] data = Convert.FromBase64String(fileName);
                return Encoding.UTF8.GetString(data);
            }
            catch
            {
                return fileName;
            }
        }

        private bool IsLikelyImageFile(string filePath)
        {
            try
            {
                byte[] header = new byte[8];
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    fs.Read(header, 0, header.Length);
                }

                if (header.Take(3).SequenceEqual(new byte[] { 0xFF, 0xD8, 0xFF })) return true; // JPEG
                if (header.Take(8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })) return true; // PNG
                if (header.Take(6).SequenceEqual(new byte[] { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 }) ||
                    header.Take(6).SequenceEqual(new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 })) return true; // GIF
                if (header.Take(2).SequenceEqual(new byte[] { 0x42, 0x4D })) return true; // BMP
                if (header.Take(4).SequenceEqual(new byte[] { 0x49, 0x49, 0x2A, 0x00 }) ||
                    header.Take(4).SequenceEqual(new byte[] { 0x4D, 0x4D, 0x00, 0x2A })) return true; // TIFF

                return false;
            }
            catch
            {
                return false;
            }
        }

        private bool IsLikelyAudioFile(string filePath)
        {
            try
            {
                byte[] header = new byte[12];
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    fs.Read(header, 0, header.Length);
                }

                if (header.Take(3).SequenceEqual(new byte[] { 0x49, 0x44, 0x33 }) ||
                    header.Take(2).SequenceEqual(new byte[] { 0xFF, 0xFB })) return true; // MP3
                if (header.Take(4).SequenceEqual(new byte[] { 0x52, 0x49, 0x46, 0x46 }) &&
                    header.Skip(8).Take(4).SequenceEqual(new byte[] { 0x57, 0x41, 0x56, 0x45 })) return true; // WAV
                if (header.Take(4).SequenceEqual(new byte[] { 0x4F, 0x67, 0x67, 0x53 })) return true; // OGG

                return false;
            }
            catch
            {
                return false;
            }
        }

        private bool IsLikelyXmlFile(string filePath)
        {
            try
            {
                byte[] buffer = new byte[128]; // Read first 128 bytes to check for XML-like content
                int bytesRead;
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    bytesRead = fs.Read(buffer, 0, buffer.Length);
                }

                if (bytesRead < 2) return false; // Need at least "<" for XML-like content
                string content = Encoding.UTF8.GetString(buffer, 0, bytesRead).TrimStart();
                // Check for XML declaration or any XML-like tag (e.g., <images>)
                return content.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase) ||
                       content.StartsWith("<") && content.Contains(">");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при проверке XML файла {filePath}: {ex.Message}");
                return false;
            }
        }

        private void FileListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (fileListBox.SelectedItem == null) return;

            string filePath = ((dynamic)fileListBox.SelectedItem).Path;
            string fileName = Path.GetFileName(filePath);
            string decodedName = ((dynamic)fileListBox.SelectedItem).DecodedName;

            try
            {
                fileNameLabel.Text = $"{fileName}\n(Декодировано: {decodedName})";

                if (IsLikelyImageFile(filePath))
                {
                    imagePreview.Visible = true;
                    xmlContentTextBox.Visible = false;
                    playStopButton.Enabled = false;
                    volumeTrackBar.Enabled = false;
                    using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    {
                        imagePreview.Image = Image.FromStream(fs);
                    }
                }
                else if (IsLikelyXmlFile(filePath))
                {
                    imagePreview.Visible = false;
                    xmlContentTextBox.Visible = true;
                    playStopButton.Enabled = false;
                    volumeTrackBar.Enabled = false;
                    try
                    {
                        using (var reader = new StreamReader(filePath, Encoding.UTF8))
                        {
                            xmlContentTextBox.Text = reader.ReadToEnd();
                        }
                    }
                    catch (Exception ex)
                    {
                        xmlContentTextBox.Text = $"Ошибка при чтении XML: {ex.Message}";
                    }
                }
                else if (IsLikelyAudioFile(filePath))
                {
                    imagePreview.Visible = false;
                    xmlContentTextBox.Visible = false;
                    playStopButton.Enabled = true;
                    volumeTrackBar.Enabled = true;
                }

                StopAudio();
            }
            catch (Exception ex)
            {
                fileNameLabel.Text = fileName;
                imagePreview.Image = null;
                xmlContentTextBox.Text = "";
                MessageBox.Show($"Ошибка при загрузке файла: {ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenInExplorerButton_Click(object sender, EventArgs e)
        {
            if (fileListBox.SelectedItem == null) return;

            string filePath = ((dynamic)fileListBox.SelectedItem).Path;

            try
            {
                string directory = Path.GetDirectoryName(filePath);
                if (string.IsNullOrEmpty(directory)) return;

                // Используем Shell API для проверки открытых окон проводника
                Shell shell = (Shell)Activator.CreateInstance(Type.GetTypeFromProgID("Shell.Application"));
                ShellWindows windows = shell.Windows();
                bool found = false;

                foreach (InternetExplorer window in windows)
                {
                    // Проверяем, является ли окно проводником
                    if (window.FullName.ToLower().EndsWith("explorer.exe"))
                    {
                        ShellFolderView view = (ShellFolderView)window.Document;
                        // Получаем путь папки через Items().Item().Path
                        string folderPath = view.Folder.Items().Item().Path;
                        if (string.Equals(folderPath, directory, StringComparison.OrdinalIgnoreCase))
                        {
                            // Найдено окно с нужной папкой, сбрасываем выделение, выделяем и фокусируем новый файл
                            view.SelectItem(view.Folder.ParseName(Path.GetFileName(filePath)), 1 | 4 | 8); // SVSI_SELECT | SVSI_DESELECTOTHERS | SVSI_ENSUREVISIBLE
                            window.Visible = true; // Делаем окно видимым
                            found = true;
                            break;
                        }
                    }
                }

                // Освобождаем COM-объекты
                if (windows != null) Marshal.ReleaseComObject(windows);
                if (shell != null) Marshal.ReleaseComObject(shell);

                // Если окно не найдено, открываем новое
                if (!found)
                {
                    System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{filePath}\"");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось открыть проводник: {ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CovertFileButton_Click(object sender, EventArgs e)
        {
            if (fileListBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите файл для копирования", "Ошибка",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string sourcePath = ((dynamic)fileListBox.SelectedItem).Path;
            string originalFileName = Path.GetFileNameWithoutExtension(sourcePath);

            string extension = GetFileExtension(sourcePath);

            using (var saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = $"{extension.ToUpper()} Files (*{extension})|*{extension}|All files (*.*)|*.*";
                saveDialog.FileName = originalFileName + extension;
                saveDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop); // Default to Desktop

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        File.Copy(sourcePath, saveDialog.FileName, overwrite: true);
                        MessageBox.Show("Файл успешно скопирован!", "Успех",
                                      MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при копировании файла: {ex.Message}",
                                       "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private string GetFileExtension(string filePath)
        {
            try
            {
                byte[] header = new byte[12];
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    fs.Read(header, 0, header.Length);
                }

                if (header.Take(3).SequenceEqual(new byte[] { 0xFF, 0xD8, 0xFF })) return ".jpg";
                if (header.Take(8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })) return ".png";
                if (header.Take(6).SequenceEqual(new byte[] { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 }) ||
                    header.Take(6).SequenceEqual(new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 })) return ".gif";
                if (header.Take(2).SequenceEqual(new byte[] { 0x42, 0x4D })) return ".bmp";
                if (header.Take(4).SequenceEqual(new byte[] { 0x49, 0x49, 0x2A, 0x00 }) ||
                    header.Take(4).SequenceEqual(new byte[] { 0x4D, 0x4D, 0x00, 0x2A })) return ".tiff";
                if (header.Take(3).SequenceEqual(new byte[] { 0x49, 0x44, 0x33 }) ||
                    header.Take(2).SequenceEqual(new byte[] { 0xFF, 0xFB })) return ".mp3";
                if (header.Take(4).SequenceEqual(new byte[] { 0x52, 0x49, 0x46, 0x46 }) &&
                    header.Skip(8).Take(4).SequenceEqual(new byte[] { 0x57, 0x41, 0x56, 0x45 })) return ".wav";
                if (header.Take(4).SequenceEqual(new byte[] { 0x4F, 0x67, 0x67, 0x53 })) return ".ogg";
                if (IsLikelyXmlFile(filePath)) return ".xml";
            }
            catch
            {
            }

            return ".dat";
        }

        private void PlayStopButton_Click(object sender, EventArgs e)
        {
            if (fileListBox.SelectedItem == null) return;

            string filePath = ((dynamic)fileListBox.SelectedItem).Path;

            if (!IsLikelyAudioFile(filePath)) return;

            try
            {
                if (!isPlaying)
                {
                    // Stop any existing playback before starting new
                    StopAudio();

                    waveOut = new WaveOutEvent();
                    audioFileReader = new AudioFileReader(filePath);
                    audioFileReader.Volume = lastVolume;
                    waveOut.Init(audioFileReader);
                    waveOut.PlaybackStopped += WaveOut_PlaybackStopped; // Handle playback completion
                    waveOut.Play();
                    playStopButton.Text = "Стоп";
                    isPlaying = true;
                }
                else
                {
                    StopAudio();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка воспроизведения: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                StopAudio(); // Ensure cleanup on error
            }
        }

        private void WaveOut_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            // Reset state when playback naturally completes
            StopAudio();
        }

        private void StopAudio()
        {
            if (waveOut != null)
            {
                waveOut.Stop();
                waveOut.Dispose();
                waveOut = null;
            }
            if (audioFileReader != null)
            {
                audioFileReader.Dispose();
                audioFileReader = null;
            }
            playStopButton.Text = "Воспроизвести";
            isPlaying = false;
        }

        private void VolumeTrackBar_Scroll(object sender, EventArgs e)
        {
            lastVolume = volumeTrackBar.Value / 100f;
            if (audioFileReader != null)
            {
                audioFileReader.Volume = lastVolume;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            StopAudio();
            base.OnFormClosing(e);
        }
    }
}