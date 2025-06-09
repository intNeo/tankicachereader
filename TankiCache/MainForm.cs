using NAudio.Wave;
using SHDocVw; // Для ShellWindows
using Shell32; // Для Shell
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

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
                        IsXml = IsLikelyXmlFile(f),
                        Is3ds = IsLikely3dsFile(f),
                        IsTara = IsLikelyTaraFile(f),
                        IsAudio = false // Будет определено позже
                    })
                    .Select(f => new
                    {
                        f.Path,
                        f.DecodedName,
                        f.IsImage,
                        f.IsXml,
                        f.Is3ds,
                        f.IsTara,
                        IsAudio = !f.IsImage && !f.IsXml && !f.Is3ds && !f.IsTara && IsLikelyAudioFile(f.Path)
                    })
                    .Where(f => f.IsImage || f.IsXml || f.Is3ds || f.IsTara || f.IsAudio)
                    .OrderBy(f => f.DecodedName)
                    .ToArray();

                fileListBox.Items.Clear();
                fileListBox.DisplayMember = "DecodedName";
                fileListBox.ValueMember = "Path";
                fileListBox.Items.AddRange(files);

                if (files.Length == 0)
                {
                    MessageBox.Show("Не найдено подходящих файлов в выбранной директории.", "Информация",
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

        private bool IsLikelyXmlFile(string filePath)
        {
            try
            {
                byte[] buffer = new byte[128];
                int bytesRead;
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    bytesRead = fs.Read(buffer, 0, buffer.Length);
                }

                if (bytesRead < 2) return false;
                string content = Encoding.UTF8.GetString(buffer, 0, bytesRead).TrimStart();
                return content.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase) ||
                       content.StartsWith("<") && content.Contains(">");
            }
            catch
            {
                return false;
            }
        }

        private bool IsLikely3dsFile(string filePath)
        {
            try
            {
                byte[] header = new byte[2];
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    fs.Read(header, 0, header.Length);
                }

                return header.SequenceEqual(new byte[] { 0x4D, 0x4D }); // 3DS files start with "MM"
            }
            catch
            {
                return false;
            }
        }

        private bool IsLikelyTaraFile(string filePath)
        {
            try
            {
                byte[] header = new byte[12];
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    fs.Read(header, 0, header.Length);
                }

                // Проверяем сигнатуру TARA (на основе лога)
                if (header.Take(6).SequenceEqual(new byte[] { 0x00, 0x00, 0x00 }) &&
                    header.Skip(6).Take(6).SequenceEqual(new byte[] { 0x6C, 0x69, 0x62, 0x72, 0x61, 0x72 })) // "librar"
                    return true;
                if (header.Take(8).SequenceEqual(new byte[] { 0x00, 0x00, 0x00, 0x02, 0x00, 0x01, 0x69, 0x00 }))
                    return true;

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
                byte[] header = new byte[16];
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    fs.Read(header, 0, header.Length);
                }

                // Проверка стандартных аудиоформатов по HEX-заголовкам
                if (header.Take(3).SequenceEqual(new byte[] { 0x49, 0x44, 0x33 }) ||
                    header.Take(2).SequenceEqual(new byte[] { 0xFF, 0xFB })) return true; // MP3
                if (header.Take(4).SequenceEqual(new byte[] { 0x52, 0x49, 0x46, 0x46 }) &&
                    header.Skip(8).Take(4).SequenceEqual(new byte[] { 0x57, 0x41, 0x56, 0x45 })) return true; // WAV
                if (header.Take(4).SequenceEqual(new byte[] { 0x4F, 0x67, 0x67, 0x53 })) return true; // OGG
                if (header.Take(4).SequenceEqual(new byte[] { 0x66, 0x4C, 0x61, 0x43 })) return true; // FLAC
                if (header.Take(4).SequenceEqual(new byte[] { 0x41, 0x44, 0x49, 0x46 }) ||
                    header.Take(2).SequenceEqual(new byte[] { 0xFF, 0xF1 }) ||
                    header.Take(2).SequenceEqual(new byte[] { 0xFF, 0xF9 })) return true; // AAC
                if (header.Take(16).SequenceEqual(new byte[] { 0x30, 0x26, 0xB2, 0x75, 0x8E, 0x66, 0xCF, 0x11,
                                                              0xA6, 0xD9, 0x00, 0xAA, 0x00, 0x62, 0xCE, 0x6C })) return true; // WMA

                //// Проверка через NAudio для файлов с расширениями .mp3, .wav, .ogg, .flac, .aac, .wma, .bin, .bas
                try
                {
                    using (var reader = new AudioFileReader(filePath))
                    {
                        // Если загрузка прошла без ошибок, считаем файл аудио
                        return true;
                    }
                }
                catch
                {
                    // Логируем HEX-заголовок для отладки
                    string hexHeader = BitConverter.ToString(header).Replace("-", " ");
                    System.Diagnostics.Debug.WriteLine($"Неизвестный аудиофайл: {filePath}, HEX: {hexHeader}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при проверке аудиофайла {filePath}: {ex.Message}");
                return false;
            }
        }

        private void FileListBox_Click(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void FileListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (fileListBox.SelectedItem == null) return;

            string filePath = ((dynamic)fileListBox.SelectedItem).Path;
            string fileName = Path.GetFileName(filePath); // Объявляем fileName здесь
            try
            {
                string decodedName = TryDecodeFileName(Path.GetFileNameWithoutExtension(filePath));

                fileNameLabel.Text = $"Оригинал: {fileName}\nДекодировано: {decodedName}";

                if (((dynamic)fileListBox.SelectedItem).IsImage)
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
                else if (((dynamic)fileListBox.SelectedItem).IsXml)
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
                else if (((dynamic)fileListBox.SelectedItem).IsAudio)
                {
                    imagePreview.Visible = false;
                    xmlContentTextBox.Visible = false;
                    playStopButton.Enabled = true;
                    volumeTrackBar.Enabled = true;
                }
                else
                {
                    imagePreview.Visible = false;
                    xmlContentTextBox.Visible = true;
                    xmlContentTextBox.Text = "Предпросмотр для этого типа файла недоступен.";
                    playStopButton.Enabled = false;
                    volumeTrackBar.Enabled = false;
                }

                StopAudio();
            }
            catch (Exception ex)
            {
                fileNameLabel.Text = $"Ошибка: {fileName}"; // Теперь fileName доступна
                imagePreview.Image = null;
                xmlContentTextBox.Text = "";
                MessageBox.Show($"Ошибка при загрузке файла: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                Shell shell = (Shell)Activator.CreateInstance(Type.GetTypeFromProgID("Shell.Application"));
                ShellWindows windows = shell.Windows();
                bool found = false;

                foreach (InternetExplorer window in windows)
                {
                    if (window.FullName.ToLower().EndsWith("explorer.exe"))
                    {
                        ShellFolderView view = (ShellFolderView)window.Document;
                        string folderPath = view.Folder.Items().Item().Path;
                        if (string.Equals(folderPath, directory, StringComparison.OrdinalIgnoreCase))
                        {
                            view.SelectItem(view.Folder.ParseName(Path.GetFileName(filePath)), 1 | 4 | 8);
                            window.Visible = true;
                            found = true;
                            break;
                        }
                    }
                }

                Marshal.ReleaseComObject(windows);
                Marshal.ReleaseComObject(shell);

                if (!found)
                {
                    System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{filePath}\"");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось открыть проводник: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CopyFileButton_Click(object sender, EventArgs e)
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
                saveDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

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
                        MessageBox.Show($"Ошибка при копировании файла: {ex.Message}", "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private string GetFileExtension(string filePath)
        {
            try
            {
                byte[] header = new byte[16];
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
                if (header.Take(4).SequenceEqual(new byte[] { 0x66, 0x4C, 0x61, 0x43 })) return ".flac";
                if (header.Take(4).SequenceEqual(new byte[] { 0x41, 0x44, 0x49, 0x46 }) ||
                    header.Take(2).SequenceEqual(new byte[] { 0xFF, 0xF1 }) ||
                    header.Take(2).SequenceEqual(new byte[] { 0xFF, 0xF9 })) return ".aac";
                if (header.Take(16).SequenceEqual(new byte[] { 0x30, 0x26, 0xB2, 0x75, 0x8E, 0x66, 0xCF, 0x11,
                                                              0xA6, 0xD9, 0x00, 0xAA, 0x00, 0x62, 0xCE, 0x6C })) return ".wma";
                if (IsLikelyXmlFile(filePath)) return ".xml";
                if (IsLikely3dsFile(filePath)) return ".3ds";
                if (IsLikelyTaraFile(filePath)) return ".tara";
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

            if (!((dynamic)fileListBox.SelectedItem).IsAudio) return;

            try
            {
                if (!isPlaying)
                {
                    StopAudio();

                    waveOut = new WaveOutEvent();
                    audioFileReader = new AudioFileReader(filePath);
                    audioFileReader.Volume = lastVolume;
                    waveOut.Init(audioFileReader);
                    waveOut.PlaybackStopped += WaveOut_PlaybackStopped;
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
                StopAudio();
            }
        }

        private void WaveOut_PlaybackStopped(object sender, StoppedEventArgs e)
        {
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