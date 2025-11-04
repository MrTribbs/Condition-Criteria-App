namespace Condition_Criteria_App
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using Newtonsoft.Json;
    using System.Threading.Tasks;
    using System.Windows.Forms;


    public partial class Form1 : Form
    {
        // Data lists
        private List<AreaEntry> aEntries;
        private List<CEntry> criteriaEntries;
        private List<CNotesEntry> cNotesEntries;
        private List<ANotesEntry> aNotesEntries;
        private List<SummaryEntry> summaryEntries;

        // Cached checkboxes array (reuse instead of recreating arrays)
        private readonly CheckBox[] _criteriaBoxes;

        // Lookup caches for O(1) access
        private Dictionary<(string Area, string Name), AreaEntry> _areaLookup = new();
        private Dictionary<string, List<string>> _areaToNames = new();
        private Dictionary<string, CEntry> _criteriaByDc = new();
        private Dictionary<string, CNotesEntry> _cNotesByDc = new();
        private Dictionary<string, ANotesEntry> _aNotesByDc = new();

        // Cached list of distinct areas for dropdown
        private string[] _distinctAreas = Array.Empty<string>();

        // Shared HttpClient to avoid socket exhaustion / allocations
        private static readonly HttpClient s_httpClient = new HttpClient();

        public Form1()
        {
            InitializeComponent();

            // Initialize the checkbox cache (designer fields must already exist)
            _criteriaBoxes = new[] { checkBoxR1, checkBoxR2, checkBoxR3, checkBoxR4, checkBoxR5, checkBoxR6, checkBoxR7, checkBoxR8 };

            // Load data from your classes (guard against null)
            aEntries = AreaData.Entries ?? new List<AreaEntry>();
            criteriaEntries = CriteriaData.Entries ?? new List<CEntry>();
            cNotesEntries = CNotesData.Entries ?? new List<CNotesEntry>();
            aNotesEntries = ANotesData.Entries ?? new List<ANotesEntry>();

            summaryEntries = new List<SummaryEntry>();

            // Build fast lookup caches
            BuildCaches();

            // Wire up events
            comboBoxArea.SelectedIndexChanged += comboBoxArea_SelectedIndexChanged;
            comboBoxName.SelectedIndexChanged += comboBoxName_SelectedIndexChanged;
            buttonAddToSummary.Click += buttonAddToSummary_Click;
            buttonCopySummary.Click += buttonCopySummary_Click;
            buttonReset.Click += buttonReset_Click;

            foreach (var cb in _criteriaBoxes)
            {
                cb.CheckedChanged += CriteriaCheckBox_CheckedChanged;
            }

            // Start with checkboxes, notes and summary controls hidden
            SetCriteriaBoxesVisible(false);
            EnsureSummaryVisible(false);

            // Also hide notes and their labels initially
            tbCNotes.Visible = false;
            tbANotes.Visible = false;
            label1.Visible = false; // "Criteria Notes" label
            label2.Visible = false; // "Condition Notes" label
        }

        private void BuildCaches()
        {
            // Area lookup by (Area, Name)
            _areaLookup = aEntries
                .Where(e => e.Area != null && e.Name != null)
                .ToDictionary(e => (e.Area, e.Name), e => e);

            // Area -> distinct names
            _areaToNames = aEntries
                .GroupBy(e => e.Area)
                .ToDictionary(g => g.Key ?? "", g => g.Select(x => x.Name).Distinct().OrderBy(n => n).ToList());

            // Distinct areas
            _distinctAreas = _areaToNames.Keys.Where(k => !string.IsNullOrEmpty(k)).OrderBy(k => k).ToArray();

            // DC lookups
            _criteriaByDc = criteriaEntries
                .Where(c => c.DC != null)
                .ToDictionary(c => c.DC, c => c);

            _cNotesByDc = cNotesEntries
                .Where(c => c.DC != null)
                .ToDictionary(c => c.DC, c => c);

            _aNotesByDc = aNotesEntries
                .Where(a => a.DC != null)
                .ToDictionary(a => a.DC, a => a);
        }

        private void Form1_Load(object? sender, EventArgs e)
        {
            PopulateAreaDropdown();
        }

        private async void btnCheckUpdate_Click(object sender, EventArgs e)
        {
            try
            {
                await CheckForUpdateAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to check for updates: {ex.Message}", "Error");
            }
        }

        private async Task CheckForUpdateAsync()
        {
            string manifestUrl = "https://github.com/MrTribbs/Condition-Criteria-App/releases/latest/download/update.json";

            using HttpClient client = new();
            string json = await client.GetStringAsync(manifestUrl);
            // MessageBox.Show($"Raw JSON: {json}");

            if (string.IsNullOrWhiteSpace(json))
            {
                MessageBox.Show("Update manifest is empty or could not be downloaded.", "Update Error");
                return;
            }

            var updateInfo = JsonConvert.DeserializeObject<UpdateManifest>(json);
            // MessageBox.Show($"Parsed URL: {updateInfo?.Url}");

            if (updateInfo == null)
            {
                MessageBox.Show("Update information is invalid.", "Update Error");
                return;
            }

            var currentVersion = Application.ProductVersion.Split('+')[0].Trim();

            if (updateInfo.Version.Trim() != currentVersion)
            {
                DialogResult result = MessageBox.Show(
                    $"New version {updateInfo.Version} available.\nDo you want to download it?",
                    "Update Available",
                    MessageBoxButtons.YesNo);

                if (result == DialogResult.Yes && !string.IsNullOrWhiteSpace(updateInfo.Url))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = updateInfo.Url,
                        UseShellExecute = true
                    });
                }
            }
            else
            {
                MessageBox.Show("You are running the latest version.", "No Update Available");
            }
        }

        public class UpdateManifest
        {
            public string Version { get; set; }
            public string Notes { get; set; }
            public string Url { get; set; }
        }

        private void PopulateAreaDropdown()
        {
            comboBoxArea.BeginUpdate();
            try
            {
                comboBoxArea.Items.Clear();
                if (_distinctAreas.Length > 0)
                {
                    comboBoxArea.Items.AddRange(_distinctAreas);
                }
            }
            finally
            {
                comboBoxArea.EndUpdate();
            }
        }

        private string? SelectedArea => comboBoxArea.SelectedItem?.ToString();
        private string? SelectedName => comboBoxName.SelectedItem?.ToString();

        private void comboBoxArea_SelectedIndexChanged(object? sender, EventArgs e)
        {
            string? selectedArea = comboBoxArea.SelectedItem?.ToString();

            // Clear items and any selected/displayed value so the UI updates immediately
            comboBoxName.BeginUpdate();
            try
            {
                comboBoxName.Items.Clear();
                comboBoxName.SelectedIndex = -1;
                comboBoxName.SelectedItem = null;
                comboBoxName.Text = "";

                if (!string.IsNullOrEmpty(selectedArea) && _areaToNames.TryGetValue(selectedArea, out var names) && names.Count > 0)
                {
                    comboBoxName.Items.AddRange(names.ToArray());
                }
            }
            finally
            {
                comboBoxName.EndUpdate();
            }

            ClearCriteriaCheckboxes();
            lblRating.Text = "";
        }

        private void comboBoxName_SelectedIndexChanged(object? sender, EventArgs e)
        {
            string? selectedArea = comboBoxArea.SelectedItem?.ToString();
            string? selectedName = comboBoxName.SelectedItem?.ToString();

            if (!string.IsNullOrEmpty(selectedArea) && !string.IsNullOrEmpty(selectedName) &&
                _areaLookup.TryGetValue((selectedArea, selectedName), out var areaEntry))
            {
                _criteriaByDc.TryGetValue(areaEntry.DC, out var criteriaEntry);

                SetCriteriaCheckbox(checkBoxR1, criteriaEntry?.R1);
                SetCriteriaCheckbox(checkBoxR2, criteriaEntry?.R2);
                SetCriteriaCheckbox(checkBoxR3, criteriaEntry?.R3);
                SetCriteriaCheckbox(checkBoxR4, criteriaEntry?.R4);
                SetCriteriaCheckbox(checkBoxR5, criteriaEntry?.R5);
                SetCriteriaCheckbox(checkBoxR6, criteriaEntry?.R6);
                SetCriteriaCheckbox(checkBoxR7, criteriaEntry?.R7);
                SetCriteriaCheckbox(checkBoxR8, criteriaEntry?.R8);
            }
            else
            {
                ClearCriteriaCheckboxes();
            }

            // Show Area Note in tbANotes
            tbANotes.Text = "";
            tbANotes.Visible = false;
            label2.Visible = false;
            if (!string.IsNullOrEmpty(selectedArea) && !string.IsNullOrEmpty(selectedName) &&
                _areaLookup.TryGetValue((selectedArea, selectedName), out var foundAreaEntry))
            {
                if (_aNotesByDc.TryGetValue(foundAreaEntry.DC, out var aNoteEntry) && !string.IsNullOrEmpty(aNoteEntry?.AreaNote))
                {
                    tbANotes.Text = aNoteEntry.AreaNote;
                    tbANotes.Visible = true;
                    label2.Visible = true;
                }
            }
        }

        private static void SetCriteriaCheckbox(CheckBox cb, string? value)
        {
            // Assign text & visibility first
            cb.Text = value ?? "";
            cb.Visible = !string.IsNullOrEmpty(value);
            cb.Checked = false;

            if (!cb.Visible)
            {
                // Reset size if hidden (optional)
                cb.AutoSize = true;
                return;
            }

            // Make the checkbox a fixed width so text can wrap.
            // Protect against null parent (designer/run time differences)
            int parentWidth = cb.Parent?.ClientSize.Width ?? cb.Width;
            int targetWidth = Math.Min(1258, Math.Max(100, parentWidth - 10));
            cb.AutoSize = false;
            cb.Width = targetWidth;

            // Measure wrapped height and set control height. Add small padding.
            var flags = TextFormatFlags.WordBreak;
            var measured = TextRenderer.MeasureText(cb.Text, cb.Font, new Size(cb.Width, int.MaxValue), flags);
            cb.Height = Math.Max(measured.Height + 8, 18);

            // Make sure the check mark/text align nicely
            cb.TextAlign = ContentAlignment.MiddleLeft;
        }

        private void ClearCriteriaCheckboxes()
        {
            foreach (var cb in _criteriaBoxes)
                SetCriteriaCheckbox(cb, null);
        }

        private void CriteriaCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            // Only respond if the checkbox is checked
            var cb = sender as CheckBox;
            if (cb == null || !cb.Checked) return;

            // Uncheck all other checkboxes (only one can be selected)
            foreach (var box in _criteriaBoxes)
            {
                if (box != cb) box.Checked = false;
            }

            // Get the selected AreaEntry via cache
            string? selectedArea = comboBoxArea.SelectedItem?.ToString();
            string? selectedName = comboBoxName.SelectedItem?.ToString();

            if (string.IsNullOrEmpty(selectedArea) || string.IsNullOrEmpty(selectedName) ||
                !_areaLookup.TryGetValue((selectedArea, selectedName), out var areaEntry))
            {
                lblRating.Text = "";
                return;
            }

            // Determine rating using helper
            string ratingText = GetRatingForBox(areaEntry, cb);
            lblRating.Text = $"Rating: {ratingText}";

            // Show Criteria Note in tbCNotes using cached CNotes
            tbCNotes.Text = "";
            tbCNotes.Visible = false;
            label1.Visible = false;

            if (_cNotesByDc.TryGetValue(areaEntry.DC, out var cNoteEntry))
            {
                var rKey = GetRKeyForBox(cb);
                var noteValue = GetCNotesValue(cNoteEntry, rKey);
                if (!string.IsNullOrEmpty(noteValue))
                {
                    tbCNotes.Text = noteValue;
                    tbCNotes.Visible = true;
                    label1.Visible = true;
                }
            }
        }

        private void buttonAddToSummary_Click(object? sender, EventArgs e)
        {
            string? selectedArea = comboBoxArea.SelectedItem?.ToString();
            string? selectedName = comboBoxName.SelectedItem?.ToString();

            if (string.IsNullOrEmpty(selectedArea) || string.IsNullOrEmpty(selectedName) ||
                !_areaLookup.TryGetValue((selectedArea, selectedName), out var areaEntry))
            {
                MessageBox.Show("Please select an area, condition, and a rating before adding to summary.");
                return;
            }

            // Find which checkbox is checked using cached array
            CheckBox? checkedBox = _criteriaBoxes.FirstOrDefault(b => b.Checked);

            if (checkedBox == null)
            {
                MessageBox.Show("Please select a rating before adding to summary.");
                return;
            }

            // Use helper to get rating string
            string ratingText = GetRatingForBox(areaEntry, checkedBox);

            summaryEntries.Add(new SummaryEntry
            {
                Area = areaEntry.Area,
                Name = areaEntry.Name,
                DC = areaEntry.DC,
                Rating = ratingText
                // Add more fields as needed
            });

            // Add to ListBox
            listSummary.Items.Add($"{areaEntry.Area} > {areaEntry.Name} > DC: {areaEntry.DC} > Rating: {ratingText}");

            // Ensure summary controls are visible once we have entries
            EnsureSummaryVisible(summaryEntries.Count > 0);

            // Calculate and display possible rating
            int possibleRating = CalculateCombinedRating(summaryEntries);
            lblPossibleRating.Text = ($"Projected Increase to {possibleRating}.");
        }

        private int CalculateCombinedRating(List<SummaryEntry> summaryEntries)
        {
            // Get ratings from summary entries (convert to int, ignore blanks)
            // Using LINQ is fine here; number of entries small.
            var ratings = summaryEntries
                .Select(e => int.TryParse(e.Rating, out int r) ? r : 0)
                .Where(r => r > 0)
                .OrderByDescending(r => r)
                .ToList();

            double combined = 0;
            foreach (var rating in ratings)
            {
                combined += (100 - combined) * (rating / 100.0);
            }

            // Round to nearest 10
            return (int)(Math.Round(combined / 10.0) * 10);
        }

        private void buttonCopySummary_Click(object? sender, EventArgs e)
        {
            if (summaryEntries.Count == 0)
            {
                MessageBox.Show("No summary entries to copy.");
                return;
            }

            // Build summary text using StringBuilder to reduce allocations
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(tb_curRatings.Text))
            {
                sb.AppendLine("Client's Current Ratings:");
                sb.AppendLine(tb_curRatings.Text);
                sb.AppendLine();
            }

            sb.AppendLine(lblPossibleRating.Text);
            foreach (var entry in summaryEntries)
            {
                sb.AppendLine($"{entry.Area} > {entry.Name} > DC: {entry.DC} > Rating: {entry.Rating}");
            }

            try
            {
                Clipboard.SetText(sb.ToString());
                MessageBox.Show("Summary copied to clipboard!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to copy summary: {ex.Message}", "Clipboard Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void buttonReset_Click(object? sender, EventArgs e)
        {
            summaryEntries.Clear();

            comboBoxArea.SelectedIndex = -1;
            comboBoxArea.Text = "";
            comboBoxName.SelectedIndex = -1;
            comboBoxName.Text = "";
            comboBoxName.Items.Clear();

            ClearCriteriaCheckboxes();
            listSummary.BeginUpdate();
            try
            {
                listSummary.Items.Clear();
            }
            finally
            {
                listSummary.EndUpdate();
            }

            tb_curRatings.Text = "";

            // Hide checkboxes and summary controls again
            SetCriteriaBoxesVisible(false);
            EnsureSummaryVisible(false);

            // Hide notes and their labels
            tbANotes.Text = "";
            tbCNotes.Text = "";
            tbANotes.Visible = false;
            tbCNotes.Visible = false;
            label1.Visible = false;
            label2.Visible = false;

            MessageBox.Show("Form reset!");
        }

        // --- Helpers ----------------------------------------------------------

        private void SetCriteriaBoxesVisible(bool visible)
        {
            foreach (var box in _criteriaBoxes)
                box.Visible = visible;
        }

        private void EnsureSummaryVisible(bool visible)
        {
            listSummary.Visible = visible;
            buttonCopySummary.Visible = visible;
            lblPossibleRating.Visible = visible;
        }

        private static string GetRKeyForBox(CheckBox cb)
        {
            if (cb == null) return null!;
            if (cb.Name.EndsWith("R1")) return "R1";
            if (cb.Name.EndsWith("R2")) return "R2";
            if (cb.Name.EndsWith("R3")) return "R3";
            if (cb.Name.EndsWith("R4")) return "R4";
            if (cb.Name.EndsWith("R5")) return "R5";
            if (cb.Name.EndsWith("R6")) return "R6";
            if (cb.Name.EndsWith("R7")) return "R7";
            if (cb.Name.EndsWith("R8")) return "R8";
            return null!;
        }

        private static string? GetCNotesValue(CNotesEntry? entry, string? rKey)
        {
            if (entry == null || rKey == null) return null;
            return rKey switch
            {
                "R1" => entry.R1,
                "R2" => entry.R2,
                "R3" => entry.R3,
                "R4" => entry.R4,
                "R5" => entry.R5,
                "R6" => entry.R6,
                "R7" => entry.R7,
                "R8" => entry.R8,
                _ => null
            };
        }

        private string GetRatingForBox(AreaEntry areaEntry, CheckBox box)
        {
            if (box == checkBoxR1) return areaEntry.R1?.ToString() ?? "";
            if (box == checkBoxR2) return areaEntry.R2?.ToString() ?? "";
            if (box == checkBoxR3) return areaEntry.R3?.ToString() ?? "";
            if (box == checkBoxR4) return areaEntry.R4?.ToString() ?? "";
            if (box == checkBoxR5) return areaEntry.R5?.ToString() ?? "";
            if (box == checkBoxR6) return areaEntry.R6?.ToString() ?? "";
            if (box == checkBoxR7) return areaEntry.R7?.ToString() ?? "";
            if (box == checkBoxR8) return areaEntry.R8?.ToString() ?? "";
            return "";
        }
    }

    // --- Data classes: mark required properties so nullable analyzer is satisfied ---

    public class AreaEntry
    {
        public required string Area { get; set; }
        public required string Name { get; set; }
        public required string DC { get; set; }
        public int? R1 { get; set; }
        public int? R2 { get; set; }
        public int? R3 { get; set; }
        public int? R4 { get; set; }
        public int? R5 { get; set; }
        public int? R6 { get; set; }
        public int? R7 { get; set; }
        public int? R8 { get; set; }
    }

    public class CEntry
    {
        public required string DC { get; set; }
        public string? R1 { get; set; }
        public string? R2 { get; set; }
        public string? R3 { get; set; }
        public string? R4 { get; set; }
        public string? R5 { get; set; }
        public string? R6 { get; set; }
        public string? R7 { get; set; }
        public string? R8 { get; set; }
    }

    public class CNotesEntry
    {
        public required string DC { get; set; }
        public string? R1 { get; set; }
        public string? R2 { get; set; }
        public string? R3 { get; set; }
        public string? R4 { get; set; }
        public string? R5 { get; set; }
        public string? R6 { get; set; }
        public string? R7 { get; set; }
        public string? R8 { get; set; }
    }

    public class ANotesEntry
    {
        public required string DC { get; set; }
        public string? AreaNote { get; set; }
    }

    public class SummaryEntry
    {
        public required string Area { get; set; }
        public required string Name { get; set; }
        public required string DC { get; set; }
        public required string Rating { get; set; }
        // Add more fields as needed
    }
}