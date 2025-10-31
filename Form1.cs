namespace Condition_Criteria_App
{
    public partial class Form1 : Form
    {
        // Data lists
        private List<AreaEntry> aEntries;
        private List<CEntry> criteriaEntries;
        private List<CNotesEntry> cNotesEntries;
        private List<ANotesEntry> aNotesEntries;
        private List<SummaryEntry> summaryEntries;

        // Helper to iterate the 8 criteria checkboxes
        private IEnumerable<CheckBox> CriteriaBoxes =>
            new[] { checkBoxR1, checkBoxR2, checkBoxR3, checkBoxR4, checkBoxR5, checkBoxR6, checkBoxR7, checkBoxR8 };

        public Form1()
        {
            InitializeComponent();

            // Load data from your classes
            aEntries = AreaData.Entries ?? [];
            criteriaEntries = CriteriaData.Entries ?? [];
            cNotesEntries = CNotesData.Entries ?? [];
            aNotesEntries = ANotesData.Entries ?? [];

            summaryEntries = [];

            // Wire up events
            comboBoxArea.SelectedIndexChanged += comboBoxArea_SelectedIndexChanged;
            comboBoxName.SelectedIndexChanged += comboBoxName_SelectedIndexChanged;
            buttonAddToSummary.Click += buttonAddToSummary_Click;
            buttonCopySummary.Click += buttonCopySummary_Click;
            buttonReset.Click += buttonReset_Click;

            foreach (var cb in CriteriaBoxes)
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

        private void Form1_Load(object? sender, EventArgs e)
        {
            PopulateAreaDropdown();
        }

        private void PopulateAreaDropdown()
        {
            comboBoxArea.Items.Clear();
            var areas = aEntries.Select(e => e.Area).Distinct().ToList();
            comboBoxArea.Items.AddRange([.. areas]);
        }

        private string? SelectedArea => comboBoxArea.SelectedItem?.ToString();
        private string? SelectedName => comboBoxName.SelectedItem?.ToString();

        private void comboBoxArea_SelectedIndexChanged(object? sender, EventArgs e)
        {
            string? selectedArea = comboBoxArea.SelectedItem?.ToString();

            // Clear items and any selected/displayed value so the UI updates immediately
            comboBoxName.Items.Clear();
            comboBoxName.SelectedIndex = -1;
            comboBoxName.SelectedItem = null;
            comboBoxName.Text = "";

            var names = aEntries
                .Where(e => e.Area == selectedArea)
                .Select(e => e.Name)
                .Distinct()
                .ToList();
            comboBoxName.Items.AddRange(names.ToArray());

            ClearCriteriaCheckboxes();
            lblRating.Text = "";
        }

        private void comboBoxName_SelectedIndexChanged(object? sender, EventArgs e)
        {
            string? selectedArea = comboBoxArea.SelectedItem?.ToString();
            string? selectedName = comboBoxName.SelectedItem?.ToString();
            var areaEntry = aEntries.FirstOrDefault(e => e.Area == selectedArea && e.Name == selectedName);

            if (areaEntry != null)
            {
                var criteriaEntry = criteriaEntries.FirstOrDefault(c => c.DC == areaEntry.DC);
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
            if (areaEntry != null)
            {
                var aNoteEntry = aNotesEntries.FirstOrDefault(a => a.DC == areaEntry.DC);
                if (!string.IsNullOrEmpty(aNoteEntry?.AreaNote))
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
            SetCriteriaCheckbox(checkBoxR1, null);
            SetCriteriaCheckbox(checkBoxR2, null);
            SetCriteriaCheckbox(checkBoxR3, null);
            SetCriteriaCheckbox(checkBoxR4, null);
            SetCriteriaCheckbox(checkBoxR5, null);
            SetCriteriaCheckbox(checkBoxR6, null);
            SetCriteriaCheckbox(checkBoxR7, null);
            SetCriteriaCheckbox(checkBoxR8, null);
        }

        private void CriteriaCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            // Only respond if the checkbox is checked
            var cb = sender as CheckBox;
            if (cb == null || !cb.Checked) return;

            // Uncheck all other checkboxes (only one can be selected)
            foreach (var box in new[] { checkBoxR1, checkBoxR2, checkBoxR3, checkBoxR4, checkBoxR5, checkBoxR6, checkBoxR7, checkBoxR8 })
            {
                if (box != cb) box.Checked = false;
            }

            // Get the selected AreaEntry
            string? selectedArea = comboBoxArea.SelectedItem?.ToString();
            string? selectedName = comboBoxName.SelectedItem?.ToString();
            var areaEntry = aEntries.FirstOrDefault(e => e.Area == selectedArea && e.Name == selectedName);

            if (areaEntry == null)
            {
                lblRating.Text = "";
                return;
            }

            // Determine which checkbox was checked and display the corresponding rating
            string ratingText = "";
            if (cb == checkBoxR1) ratingText = areaEntry.R1?.ToString() ?? "";
            else if (cb == checkBoxR2) ratingText = areaEntry.R2?.ToString() ?? "";
            else if (cb == checkBoxR3) ratingText = areaEntry.R3?.ToString() ?? "";
            else if (cb == checkBoxR4) ratingText = areaEntry.R4?.ToString() ?? "";
            else if (cb == checkBoxR5) ratingText = areaEntry.R5?.ToString() ?? "";
            else if (cb == checkBoxR6) ratingText = areaEntry.R6?.ToString() ?? "";
            else if (cb == checkBoxR7) ratingText = areaEntry.R7?.ToString() ?? "";
            else if (cb == checkBoxR8) ratingText = areaEntry.R8?.ToString() ?? "";

            lblRating.Text = $"Rating: {ratingText}";

            // Show Criteria Note in tbCNotes
            tbCNotes.Text = "";
            tbCNotes.Visible = false;
            label1.Visible = false;
            if (areaEntry != null)
            {
                string dc = areaEntry.DC;
                string? rKey = null;
                if (cb == checkBoxR1) rKey = "R1";
                else if (cb == checkBoxR2) rKey = "R2";
                else if (cb == checkBoxR3) rKey = "R3";
                else if (cb == checkBoxR4) rKey = "R4";
                else if (cb == checkBoxR5) rKey = "R5";
                else if (cb == checkBoxR6) rKey = "R6";
                else if (cb == checkBoxR7) rKey = "R7";
                else if (cb == checkBoxR8) rKey = "R8";

                var cNoteEntry = cNotesEntries.FirstOrDefault(c => c.DC == dc);
                if (cNoteEntry != null && rKey != null)
                {
                    var noteValue = typeof(CNotesEntry).GetProperty(rKey)?.GetValue(cNoteEntry) as string;
                    if (!string.IsNullOrEmpty(noteValue))
                    {
                        tbCNotes.Text = noteValue;
                    }
                }
            }
        }

        private void buttonAddToSummary_Click(object? sender, EventArgs e)
        {
            string? selectedArea = comboBoxArea.SelectedItem?.ToString();
            string? selectedName = comboBoxName.SelectedItem?.ToString();
            var areaEntry = aEntries.FirstOrDefault(e => e.Area == selectedArea && e.Name == selectedName);

            // Find which checkbox is checked
            CheckBox? checkedBox = null;
            foreach (var box in new[] { checkBoxR1, checkBoxR2, checkBoxR3, checkBoxR4, checkBoxR5, checkBoxR6, checkBoxR7, checkBoxR8 })
            {
                if (box.Checked)
                {
                    checkedBox = box;
                    break;
                }
            }

            if (areaEntry == null || checkedBox == null)
            {
                MessageBox.Show("Please select an area, condition, and a rating before adding to summary.");
                return;
            }

            // Determine which checkbox was checked and display the corresponding rating
            string ratingText = "";
            if (checkedBox == checkBoxR1) ratingText = areaEntry.R1?.ToString() ?? "";
            else if (checkedBox == checkBoxR2) ratingText = areaEntry.R2?.ToString() ?? "";
            else if (checkedBox == checkBoxR3) ratingText = areaEntry.R3?.ToString() ?? "";
            else if (checkedBox == checkBoxR4) ratingText = areaEntry.R4?.ToString() ?? "";
            else if (checkedBox == checkBoxR5) ratingText = areaEntry.R5?.ToString() ?? "";
            else if (checkedBox == checkBoxR6) ratingText = areaEntry.R6?.ToString() ?? "";
            else if (checkedBox == checkBoxR7) ratingText = areaEntry.R7?.ToString() ?? "";
            else if (checkedBox == checkBoxR8) ratingText = areaEntry.R8?.ToString() ?? "";

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
            lblPossibleRating.Text = $"Possible Rating = {possibleRating}";
        }

        private int CalculateCombinedRating(List<SummaryEntry> summaryEntries)
        {
            // Get ratings from summary entries (convert to int, ignore blanks)
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

            var summaryText = lblPossibleRating.Text + ":" + Environment.NewLine +
                string.Join(Environment.NewLine, summaryEntries.Select(e =>
                $"{e.Area} > {e.Name} > DC: {e.DC} > Rating: {e.Rating}"
            ));

            Clipboard.SetText(summaryText);
            MessageBox.Show("Summary copied to clipboard!");
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
            listSummary.Items.Clear();

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
            foreach (var box in CriteriaBoxes)
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

        private void tabPage2_Click(object sender, EventArgs e)
        {

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