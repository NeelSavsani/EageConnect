using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Sender
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            this.Load += Form1_Load;
            txtMobile.KeyPress += TxtMobile_KeyPress;
            txtMobile.KeyDown += TxtMobile_KeyDown;
            txtMobile.MouseClick += TxtMobile_MouseClick;
            txtMobile.TextChanged += TxtMobile_TextChanged;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            txtMobile.Text = "+91";
            txtMobile.SelectionStart = txtMobile.Text.Length;
            txtName.Focus();
        }

        private bool IsValidMobileNumber(string number)
        {
            // Strip the +91 prefix if present before validating
            if (number.StartsWith("+91"))
            {
                number = number.Substring(3);
            }
            return Regex.IsMatch(number, @"^[6-9][0-9]{9}$");
        }

        private void btnSendWhatsapp_Click(object sender, EventArgs e)
        {
            string mobileNumber = txtMobile.Text.Trim();
            string receiverName = txtName.Text.Trim();
            string message = txtMessage.Text.Trim();
            string filePath = lblFilePath.Text.Trim();

            // Validate mobile number
            if (!IsValidMobileNumber(mobileNumber))
            {
                MessageBox.Show("❌ Invalid mobile number.\nIt must be 10 digits and start with 6,7,8, or 9.",
                    "Validation Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                txtMobile.Focus();
                return;
            }

            // Validate receiver name
            if (string.IsNullOrWhiteSpace(receiverName))
            {
                MessageBox.Show("❌ Receiver's name cannot be empty.",
                    "Validation Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                txtName.Focus();
                return;
            }

            // Validate that at least one of message or file path is provided
            bool isMessageFilled = !string.IsNullOrWhiteSpace(message);
            bool isFileSelected = !string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath);

            if (!isMessageFilled && !isFileSelected)
            {
                MessageBox.Show("❌ Either a message must be entered or a file must be selected.",
                    "Validation Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                txtMessage.Focus();
                return;
            }

            // Log file validation
            if (isFileSelected)
            {
                Console.WriteLine("Selected file: " + filePath);
            }

            // Call Python function (always send message even if empty; file logic might be handled separately)
            bool pythonSuccess = RunPythonFunction(mobileNumber, receiverName, message, filePath);

            if (pythonSuccess)
            {
                MessageBox.Show($"✅ Message sent successfully to {receiverName} on {mobileNumber}.",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                // Clear the form
                txtName.Text = string.Empty;
                txtMobile.Text = "+91";
                txtMessage.Text = string.Empty;
                lblFilePath.Text = "No File Chosen";
                txtMobile.SelectionStart = txtMobile.Text.Length;
                txtName.Focus();
            }
            else
            {
                MessageBox.Show("❌ Failed to send message via Python script.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private bool RunPythonFunction(string mobileNumber, string receiverName, string message, string filePath)
        {
            try
            {
                string pythonExe = @"C:\Users\<YOUR USER NAME>\AppData\Local\Programs\Python\Python313\python.exe"; //File path for python
                string scriptPath = @"<FILE PATH TO THE PYTHON FILE THAT HAS GIVEN IN THE MAIN BRANCH>"; //send_whatsapp_message.py

                // Escape quotes for safety
                message = message.Replace("\"", "\\\"");
                filePath = filePath.Replace("\"", "\\\"");

                string args = $"\"{scriptPath}\" \"{mobileNumber}\" \"{receiverName}\" \"{message}\" \"{filePath}\"";

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = pythonExe,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = false
                };

                using (Process process = Process.Start(psi))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (!string.IsNullOrEmpty(output))
                        Console.WriteLine("Python Output: " + output);

                    if (!string.IsNullOrEmpty(error))
                        Console.WriteLine("Python Error: " + error);

                    if (process.ExitCode == 0)
                    {
                        return true; // Success!
                    }
                    else
                    {
                        MessageBox.Show($"Python script exited with code {process.ExitCode}.\nError: {error}",
                            "Python Script Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error running Python script: {ex.Message}",
                    "Python Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }
        }

        private void openFileDialog1_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // No additional code needed here, but you can customize if desired.
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog
            {
                Title = "Select a File",
                Filter = "All Files (*.*)|*.*"
            };

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                lblFilePath.Text = openFileDialog1.FileName;
            }
        }

        private void label7_Click(object sender, EventArgs e)
        {
            // Placeholder
        }

        // Enforce +91 lock and digit-only input
        private void TxtMobile_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (txtMobile.SelectionStart < 3)
            {
                // Block backspace/delete and non-navigation keys
                e.Handled = true;
                txtMobile.SelectionStart = txtMobile.Text.Length;
                return;
            }

            // Allow only digits
            if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void TxtMobile_KeyDown(object sender, KeyEventArgs e)
        {
            if ((e.KeyCode == Keys.Left || e.KeyCode == Keys.Back) && txtMobile.SelectionStart <= 3)
            {
                e.Handled = true;
                txtMobile.SelectionStart = txtMobile.Text.Length;
            }
        }

        private void TxtMobile_MouseClick(object sender, MouseEventArgs e)
        {
            if (txtMobile.SelectionStart < 3)
            {
                txtMobile.SelectionStart = txtMobile.Text.Length;
            }
        }

        private void TxtMobile_TextChanged(object sender, EventArgs e)
        {
            if (!txtMobile.Text.StartsWith("+91"))
            {
                txtMobile.Text = "+91";
                txtMobile.SelectionStart = txtMobile.Text.Length;
            }
        }
    }
}
