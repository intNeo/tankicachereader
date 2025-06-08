using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace ImageViewerWithBase64Names
{
    public partial class MainForm : Form
    {
        private string currentDirectory;

        public MainForm()
        {
            InitializeComponent();
            fileListBox.SelectedIndexChanged += FileListBox_SelectedIndexChanged;
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
                    .Where(IsLikelyImageFile)
                    .OrderBy(f => f)
                    .ToArray();

                fileListBox.Items.AddRange(files);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке файлов: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                if (header.Take(3).SequenceEqual(new byte[] { 0xFF, 0xD8, 0xFF })) return true;
                if (header.Take(8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })) return true;
                if (header.Take(6).SequenceEqual(new byte[] { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 }) ||
                    header.Take(6).SequenceEqual(new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 })) return true;
                if (header.Take(2).SequenceEqual(new byte[] { 0x42, 0x4D })) return true;
                if (header.Take(4).SequenceEqual(new byte[] { 0x49, 0x49, 0x2A, 0x00 }) ||
                    header.Take(4).SequenceEqual(new byte[] { 0x4D, 0x4D, 0x00, 0x2A })) return true;

                return false;
            }
            catch
            {
                return false;
            }
        }

        private void FileListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (fileListBox.SelectedItem == null) return;

            string filePath = fileListBox.SelectedItem.ToString();
            string fileName = Path.GetFileName(filePath);

            try
            {
                string decodedName = "";
                try
                {
                    byte[] data = Convert.FromBase64String(Path.GetFileNameWithoutExtension(fileName));
                    decodedName = Encoding.UTF8.GetString(data);
                }
                catch
                {
                    decodedName = "Не удалось декодировать имя файла";
                }

                fileNameLabel.Text = $"{fileName}\n(Декодировано: {decodedName})";

                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    imagePreview.Image = Image.FromStream(fs);
                }
            }
            catch (Exception ex)
            {
                fileNameLabel.Text = fileName;
                imagePreview.Image = null;
                MessageBox.Show($"Ошибка при загрузке изображения: {ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenInExplorerButton_Click(object sender, EventArgs e)
        {
            if (fileListBox.SelectedItem == null) return;

            string filePath = fileListBox.SelectedItem.ToString();

            try
            {
                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{filePath}\"");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось открыть проводник: {ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

            string sourcePath = fileListBox.SelectedItem.ToString();

            // Определяем расширение на основе содержимого файла
            string extension = GetImageExtension(sourcePath);

            using (var saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = $"Image Files (*{extension})|*{extension}|All files (*.*)|*.*";
                saveDialog.FileName = Path.GetFileNameWithoutExtension(sourcePath) + extension;

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

        private string GetImageExtension(string filePath)
        {
            try
            {
                byte[] header = new byte[8];
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
            }
            catch
            {
                // Если не удалось определить, используем общее расширение
            }

            return ".dat"; // Расширение по умолчанию, если тип не распознан
        }
    }
}