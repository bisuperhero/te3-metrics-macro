// Copyright (c) 2026 Robert Junek, bisuperhero.cz
// Published under MIT license.

// Required namespaces
using System;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;

// --- CONFIG ---
string DEFAULT_DATE_TABLE = "Date"; // Name of the default date table
string DEFAULT_DATE_COLUMN = "Date"; // Name of the default date column
string DEFAULT_DATE_COLUMN_EXAMPLE = "'Date'[Date]"; // Example of the default date column
string DEFAULT_FORMAT_STRING = "#,##0"; // Default format string for measures
string MEASURES_TABLE_NAME = "00 MEASURES"; // Name of the table for measures, the table has to exist
string COLORS_FOLDER = ".Colors"; // Name of the folder for color measures
string PREFIX_SUM = "Ʃ"; // Prefix for SUM measures
string PREFIX_COUNT = "#"; // Prefix for COUNT measures
string PREFIX_DISTINCTCOUNT = "#"; // Prefix for DISTINCTCOUNT measures
string ALT_REL_FOLDER = "xMetrics by other relationships"; // Name of the folder for measures with other relationships
string COLOR_GREEN = "#5CB85C"; // Color for positive values 
string COLOR_GREEN_BG = "#D5F5E3"; // Background color for positive values
string COLOR_RED = "#D9534F"; // Color for negative values
string COLOR_RED_BG = "#FADBD8"; // Background color for negative values

// --- GUI Controls Declaration ---
Label nameLabel;
TextBox nameTextBox;
Label nameNoteLabel;
GroupBox calcGroup;
RadioButton sumRadio, sumxRadio, noSumRadio;
RadioButton countRadio, countxRadio, noCountRadio;
RadioButton distinctCountRadio, noDistinctCountRadio;
GroupBox supportMetricsGroup;
CheckBox pmCheckBox, pqCheckBox, pyCheckBox, ppCheckBox, rtCheckBox, metricsWithColorsCheckBox;
GroupBox relGroup;
RadioButton activeOnlyRadio, activeAndSelectedInactiveRadio, selectedInactiveOnlyRadio;
Label inactiveRelLabel;
ListBox inactiveRelListBox;
Label timeCalcColumnLabel;
TextBox timeCalcColumnTextBox;
Label timeCalcColumnNoteLabel;
CheckBox hideColumnCheckBox;
Button okButton, cancelButton;
Form configForm;

ScriptHelper.WaitFormVisible = false;

// Validate the selection
if (Selected.Columns.Count == 0)
{
    Error("Please select one column.");
    return;
}
else if (Selected.Columns.Count > 1)
{
    Error("Please select only one column.");
    return;
}

// Get the selected column
var selectedColumn = Selected.Columns.First();
var selectedTable = selectedColumn.Table;

// Prepare a list of single-column relationships from the selected table
var allRelationships = Model.Relationships.OfType<SingleColumnRelationship>()
    .Where(r => r.FromTable == selectedTable || r.ToTable == selectedTable)
    .ToList();

var activeRelationships = allRelationships.Where(r => r.IsActive).ToList();
var inactiveRelationships = allRelationships.Where(r => !r.IsActive).ToList();

// Initialize GUI controls
InitGuiControls();

// Show the form
var result = configForm.ShowDialog();

if (result != DialogResult.OK)
{
    Info("Operation canceled by the user.");
    return;
}

// Collect user selections
string measureNameInput = nameTextBox.Text.Trim();

// Determine calculation methods based on selections
var calculationMethods = new List<string>();

if (sumRadio.Checked) calculationMethods.Add("SUM");
else if (sumxRadio.Checked) calculationMethods.Add("SUMX");

if (countRadio.Checked) calculationMethods.Add("COUNT");
else if (countxRadio.Checked) calculationMethods.Add("COUNTX");

if (distinctCountRadio.Checked) calculationMethods.Add("DISTINCTCOUNT");

if (calculationMethods.Count == 0)
{
    Error("Please select at least one calculation method.");
    return;
}

// Collect supporting metrics selections
bool createPM = pmCheckBox.Checked;
bool createPQ = pqCheckBox.Checked;
bool createPY = pyCheckBox.Checked;
bool createPP = ppCheckBox.Checked;
bool createRT = rtCheckBox.Checked;
bool createMetricsWithColors = metricsWithColorsCheckBox.Checked;

// Check if any time-based metrics are selected
bool timeMetricsSelected = createPM || createPQ || createPY || createPP || createRT;

// Time calculation column
string dateTableName = "";
string dateColumnName = "";
string dateColumnDax = "";

if (timeMetricsSelected)
{
    string timeCalcColumnInput = timeCalcColumnTextBox.Text.Trim();
    if (string.IsNullOrEmpty(timeCalcColumnInput))
    {
        Error("Please provide a time calculation column.");
        return;
    }

    // Parse input to get table and column names
    var parts = timeCalcColumnInput.Split('[');
    if (parts.Length != 2 || !parts[1].EndsWith("]"))
    {
        Error("Invalid time calculation column format. Use 'TableName'[ColumnName].");
        return;
    }
    dateTableName = parts[0].Trim('\'');
    dateColumnName = parts[1].TrimEnd(']').Trim();

    // Check if the table and column exist
    var dateTable = Model.Tables.FirstOrDefault(t => t.Name == dateTableName);
    if (dateTable == null)
    {
        Error($"Table '{dateTableName}' does not exist.");
        return;
    }
    var dateColumn = dateTable.Columns.FirstOrDefault(c => c.Name == dateColumnName);
    if (dateColumn == null)
    {
        Error($"Column '{dateColumnName}' does not exist in table '{dateTableName}'.");
        return;
    }
    // Check if the column is of data type Date
    if (dateColumn.DataType != DataType.DateTime)
    {
        Error($"Column '{dateTableName}'[{dateColumnName}] is not of data type Date.");
        return;
    }

    dateColumnDax = $"'{dateTableName}'[{dateColumnName}]";
}

// Relationship selection
var selectedRelationshipOption = activeOnlyRadio.Checked ? "ActiveOnly" :
    activeAndSelectedInactiveRadio.Checked ? "ActiveAndSelectedInactive" :
    "SelectedInactiveOnly";

var selectedInactiveRelationships = new List<SingleColumnRelationship>();
if (inactiveRelListBox.Enabled)
{
    foreach (SingleColumnRelationship rel in inactiveRelListBox.SelectedItems)
    {
        selectedInactiveRelationships.Add(rel);
    }
}

// Prepare the list of relationships to use
var relationshipsToUse = new List<SingleColumnRelationship>();

if (selectedRelationshipOption == "ActiveOnly")
{
    relationshipsToUse.AddRange(activeRelationships);
}
else if (selectedRelationshipOption == "ActiveAndSelectedInactive")
{
    relationshipsToUse.AddRange(activeRelationships);
    relationshipsToUse.AddRange(selectedInactiveRelationships);
}
else if (selectedRelationshipOption == "SelectedInactiveOnly")
{
    relationshipsToUse.AddRange(selectedInactiveRelationships);
}

// Hide original column if selected
bool hideOriginalColumn = hideColumnCheckBox.Checked;
if (hideOriginalColumn)
{
    selectedColumn.IsHidden = true;
}

// Ensure color measures exist
if (createMetricsWithColors)
{
    var colorsTable = Model.Tables.FirstOrDefault(t => t.Name == MEASURES_TABLE_NAME);
    if (colorsTable == null)
    {
        Error($"Table '{MEASURES_TABLE_NAME}' does not exist. Please create it manually in your model.");
        return;
    }
    var colorsFolder = COLORS_FOLDER;

    void EnsureColorMeasure(string measureName, string colorValue)
    {
        var measure = colorsTable.Measures.FirstOrDefault(m => m.Name == measureName);
        if (measure == null)
        {
            measure = colorsTable.AddMeasure(
                measureName,
                $"\"{colorValue}\"",
                colorsFolder
            );
            measure.FormatString = "Text";
        }
    }

    EnsureColorMeasure("_color_green", COLOR_GREEN);
    EnsureColorMeasure("_color_green_bg", COLOR_GREEN_BG);
    EnsureColorMeasure("_color_red", COLOR_RED);
    EnsureColorMeasure("_color_red_bg", COLOR_RED_BG);
}

// Determine format string based on data type
var formatString = DEFAULT_FORMAT_STRING;

// For each calculation method, create base measures
foreach (var method in calculationMethods)
{
    // Determine measure name
    string baseMeasureName;
    if (!string.IsNullOrEmpty(measureNameInput))
    {
        baseMeasureName = $"{GetMeasurePrefix(method)} {measureNameInput}";
    }
    else
    {
        baseMeasureName = $"{GetMeasurePrefix(method)} {selectedColumn.Name}";
    }

    // Adjust measure name for COUNT/COUNTX with suffix
    if (method == "COUNT" || method == "COUNTX")
    {
        baseMeasureName += " (non-distinct)";
    }

    // Create the base measure
    string daxExpression = GetDaxExpression(method, selectedColumn);
    var baseMeasure = selectedTable.AddMeasure(
        baseMeasureName,
        daxExpression,
        $"{selectedColumn.DisplayFolder}\\{baseMeasureName}"
    );
    baseMeasure.FormatString = formatString;
    baseMeasure.Description = $"This measure is the {method} of {selectedColumn.DaxObjectFullName}.";

    // Create supporting measures if selected
    if (createPM || createPQ || createPY || createPP || createRT)
    {
        // Pass dateColumnDax to the function
        CreateSupportingMeasures(baseMeasure, baseMeasureName, selectedColumn, formatString, method, dateColumnDax, null);
    }

    // Create measures for inactive relationships if any
    if (selectedInactiveRelationships.Count > 0)
    {
        // Create folder for measures with other relationships
        string altFolder = $"{selectedColumn.DisplayFolder}\\{ALT_REL_FOLDER}";

        foreach (var rel in selectedInactiveRelationships)
        {
            // Measure name with relationship
            string relMeasureName = $"{baseMeasureName} by {rel.FromColumn.Name}";

            // Create DAX expression using USERELATIONSHIP
            string relationshipFilter = $", USERELATIONSHIP('{rel.FromTable.Name}'[{rel.FromColumn.Name}], '{rel.ToTable.Name}'[{rel.ToColumn.Name}])";
            string relDaxExpression = $"CALCULATE({daxExpression}{relationshipFilter})";

            var newMeasure = selectedTable.AddMeasure(
                relMeasureName,
                relDaxExpression,
                $"{altFolder}\\{relMeasureName}"
            );
            newMeasure.FormatString = formatString;
            newMeasure.Description = $"This measure is the {method} of {selectedColumn.DaxObjectFullName} using relationship between {rel.FromTable.Name}.{rel.FromColumn.Name} and {rel.ToTable.Name}.{rel.ToColumn.Name}.";

            // Create supporting measures if selected
            if (createPM || createPQ || createPY || createPP || createRT)
            {
                CreateSupportingMeasures(newMeasure, relMeasureName, selectedColumn, formatString, method, dateColumnDax, $"{altFolder}\\{relMeasureName}");
            }
        }
    }
}

// --- GUI Initialization ---
void InitGuiControls()
{
    configForm = new Form()
    {
        Text = "Measure Configuration",
        Width = 500,
        Height = 750,
        StartPosition = FormStartPosition.CenterScreen
    };
    nameLabel = new Label() { Left = 10, Top = 10, Width = 460, Text = "Measure Name (optional):" };
    nameTextBox = new TextBox() { Left = 10, Top = 30, Width = 460 };
    nameNoteLabel = new Label()
    {
        Left = 10,
        Top = 55,
        Width = 460,
        Text = "If left blank, the measure names will be generated from the column name.",
        ForeColor = System.Drawing.Color.Gray
    };

    // Calculation Method Selection
    calcGroup = new GroupBox()
    {
        Left = 10,
        Top = 90,
        Width = 460,
        Height = 140,
        Text = "Calculation Methods"
    };

    // SUM/SUMX Radiobuttons
    Label sumLabel = new Label() { Left = 10, Top = 15, Width = 100, Text = "SUM metric:" };
    sumRadio = new RadioButton() { Left = 10, Top = 40, Width = 100, Text = "SUM" };
    sumxRadio = new RadioButton() { Left = 10, Top = 65, Width = 100, Text = "SUMX" };
    noSumRadio = new RadioButton() { Left = 10, Top = 90, Width = 100, Text = "Without SUM", Checked = true };

    // Group SUM options
    var sumGroup = new GroupBox() { Left = 5, Top = 15, Width = 130, Height = 120 };
    sumGroup.Controls.Add(sumLabel);
    sumGroup.Controls.Add(sumRadio);
    sumGroup.Controls.Add(sumxRadio);
    sumGroup.Controls.Add(noSumRadio);

    // COUNT/COUNTX Radiobuttons
    Label countLabel = new Label() { Left = 10, Top = 15, Width = 100, Text = "COUNT metric:" };
    countRadio = new RadioButton() { Left = 10, Top = 40, Width = 100, Text = "COUNT" };
    countxRadio = new RadioButton() { Left = 10, Top = 65, Width = 100, Text = "COUNTX" };
    noCountRadio = new RadioButton() { Left = 10, Top = 90, Width = 100, Text = "Without COUNT", Checked = true };

    // Group COUNT options
    var countGroup = new GroupBox() { Left = 140, Top = 15, Width = 130, Height = 120 };
    countGroup.Controls.Add(countLabel);
    countGroup.Controls.Add(countRadio);
    countGroup.Controls.Add(countxRadio);
    countGroup.Controls.Add(noCountRadio);

    // DISTINCTCOUNT Radiobuttons
    Label distinctCountLabel = new Label() { Left = 10, Top = 15, Width = 150, Text = "DISTINCTCOUNT metric:" };
    distinctCountRadio = new RadioButton() { Left = 10, Top = 40, Width = 150, Text = "DISTINCTCOUNT" };
    noDistinctCountRadio = new RadioButton() { Left = 10, Top = 65, Width = 150, Text = "Without DISTINCTCOUNT", Checked = true };

    // Group DISTINCTCOUNT options
    var distinctCountGroup = new GroupBox() { Left = 275, Top = 15, Width = 180, Height = 120 };
    distinctCountGroup.Controls.Add(distinctCountLabel);
    distinctCountGroup.Controls.Add(distinctCountRadio);
    distinctCountGroup.Controls.Add(noDistinctCountRadio);

    // Add groups to calculation group
    calcGroup.Controls.Add(sumGroup);
    calcGroup.Controls.Add(countGroup);
    calcGroup.Controls.Add(distinctCountGroup);

    // Supporting Metrics Selection
    supportMetricsGroup = new GroupBox()
    {
        Left = 10,
        Top = 240,
        Width = 460,
        Height = 80,
        Text = "Supporting Metrics"
    };

    pmCheckBox = new CheckBox() { Left = 10, Top = 20, Width = 100, Text = "PM", Checked = false };
    pqCheckBox = new CheckBox() { Left = 120, Top = 20, Width = 100, Text = "PQ", Checked = false };
    pyCheckBox = new CheckBox() { Left = 230, Top = 20, Width = 100, Text = "PY", Checked = false };
    ppCheckBox = new CheckBox() { Left = 340, Top = 20, Width = 100, Text = "PP", Checked = false };
    rtCheckBox = new CheckBox() { Left = 10, Top = 50, Width = 100, Text = "RT", Checked = false };
    metricsWithColorsCheckBox = new CheckBox() { Left = 120, Top = 50, Width = 150, Text = "Metrics with colors", Checked = false };
    


    var selectAllLink = new LinkLabel()
    {
        Left = ppCheckBox.Left,
        Top = ppCheckBox.Top + ppCheckBox.Height + 2,
        Width = 100,
        Text = "Select all",
        LinkColor = System.Drawing.Color.Blue,
        ActiveLinkColor = System.Drawing.Color.Blue,
        VisitedLinkColor = System.Drawing.Color.Blue
    };

    selectAllLink.Click += (s, e) =>
    {
        pmCheckBox.Checked = true;
        pqCheckBox.Checked = true;
        pyCheckBox.Checked = true;
        ppCheckBox.Checked = true;
        rtCheckBox.Checked = true;
        metricsWithColorsCheckBox.Checked = true;
    };


    supportMetricsGroup.Controls.Add(pmCheckBox);
    supportMetricsGroup.Controls.Add(pqCheckBox);
    supportMetricsGroup.Controls.Add(pyCheckBox);
    supportMetricsGroup.Controls.Add(ppCheckBox);
    supportMetricsGroup.Controls.Add(rtCheckBox);
    supportMetricsGroup.Controls.Add(metricsWithColorsCheckBox);
    supportMetricsGroup.Controls.Add(selectAllLink);


    // Relationship Options
    relGroup = new GroupBox()
    {
        Left = 10,
        Top = 330,
        Width = 460,
        Height = 220,
        Text = "Relationship Options"
    };

    activeOnlyRadio = new RadioButton()
    {
        Left = 10,
        Top = 20,
        Width = 400,
        Text = "Create measures for active relationships",
        Checked = true
    };

    activeAndSelectedInactiveRadio = new RadioButton()
    {
        Left = 10,
        Top = 45,
        Width = 400,
        Text = "Create measures for active and selected inactive relationships"
    };

    selectedInactiveOnlyRadio = new RadioButton()
    {
        Left = 10,
        Top = 70,
        Width = 400,
        Text = "Create measures only for selected inactive relationships"
    };

    // Listbox for inactive relationships
    inactiveRelLabel = new Label() { Left = 10, Top = 100, Width = 440, Text = "Select inactive relationships:" };
    inactiveRelListBox = new ListBox()
    {
        Left = 10,
        Top = 125,
        Width = 440,
        Height = 90,
        SelectionMode = SelectionMode.MultiExtended
    };

    // Populate the listbox with inactive relationships
    foreach (var rel in inactiveRelationships)
    {
        var relName = $"{rel.FromTable.Name}.{rel.FromColumn.Name} -> {rel.ToTable.Name}.{rel.ToColumn.Name}";
        rel.Name = relName; // Set the Name property for display
        inactiveRelListBox.Items.Add(rel);
    }
    inactiveRelListBox.DisplayMember = "Name";

    // Disable listbox by default
    inactiveRelListBox.Enabled = false;
    inactiveRelLabel.Enabled = false;

    // Enable/disable listbox based on selected radio button
    activeAndSelectedInactiveRadio.CheckedChanged += (sender, e) =>
    {
        if (activeAndSelectedInactiveRadio.Checked)
        {
            inactiveRelListBox.Enabled = true;
            inactiveRelLabel.Enabled = true;
        }
    };
    selectedInactiveOnlyRadio.CheckedChanged += (sender, e) =>
    {
        if (selectedInactiveOnlyRadio.Checked)
        {
            inactiveRelListBox.Enabled = true;
            inactiveRelLabel.Enabled = true;
        }
    };
    activeOnlyRadio.CheckedChanged += (sender, e) =>
    {
        if (activeOnlyRadio.Checked)
        {
            inactiveRelListBox.Enabled = false;
            inactiveRelLabel.Enabled = false;
        }
    };

    // Add controls to relationship group
    relGroup.Controls.Add(activeOnlyRadio);
    relGroup.Controls.Add(activeAndSelectedInactiveRadio);
    relGroup.Controls.Add(selectedInactiveOnlyRadio);
    relGroup.Controls.Add(inactiveRelLabel);
    relGroup.Controls.Add(inactiveRelListBox);

    // Time Calculation Column Input (always displayed)
    timeCalcColumnLabel = new Label() { Left = 10, Top = 560, Width = 460, Text = "Time calculation column:" };
    timeCalcColumnTextBox = new TextBox() { Left = 10, Top = 585, Width = 460 };
    timeCalcColumnNoteLabel = new Label()
    {
        Left = 10,
        Top = 610,
        Width = 460,
        Height = 50,
        ForeColor = System.Drawing.Color.Gray
    };

    // Check if default date column exists
    bool dateColumnExists = Model.Tables.Any(t => t.Name == DEFAULT_DATE_TABLE && t.Columns.Any(c => c.Name == DEFAULT_DATE_COLUMN));
    if (dateColumnExists)
    {
        timeCalcColumnTextBox.Text = DEFAULT_DATE_COLUMN_EXAMPLE;
    }
    else
    {
        timeCalcColumnTextBox.Text = "";
        timeCalcColumnNoteLabel.Text = $"Expected column {DEFAULT_DATE_COLUMN_EXAMPLE} for time calculations is missing.\nFill out the name of the column you want to use (e.g., 'DateDim'[DATE_PK]).";
    }

    // Add time calculation controls to form
    configForm.Controls.Add(timeCalcColumnLabel);
    configForm.Controls.Add(timeCalcColumnTextBox);
    configForm.Controls.Add(timeCalcColumnNoteLabel);

    // Hide Original Column Checkbox
    hideColumnCheckBox = new CheckBox()
    {
        Left = 10,
        Top = 670,
        Width = 180,
        Text = "Hide original column"
    };
    configForm.Controls.Add(hideColumnCheckBox);

    // OK, Cancel, and Help buttons
    okButton = new Button() { Text = "OK", Left = 290, Width = 80, Top = 670 };
    cancelButton = new Button() { Text = "Cancel", Left = 380, Width = 80, Top = 670 };
    Button helpButton = new Button() { Text = "Help", Left = 200, Width = 80, Top = 670 };

    okButton.Click += (sender, e) =>
    {
        configForm.DialogResult = DialogResult.OK;
        configForm.Close();
    };

    cancelButton.Click += (sender, e) =>
    {
        configForm.DialogResult = DialogResult.Cancel;
        configForm.Close();
    };

    helpButton.Click += (sender, e) =>
    {
        var url = "https://github.com/bisuperhero/TabularEditor-MetricCreator/wiki";
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    };


    // Add controls to form
    configForm.Controls.Add(nameLabel);
    configForm.Controls.Add(nameTextBox);
    configForm.Controls.Add(nameNoteLabel);
    configForm.Controls.Add(calcGroup);
    configForm.Controls.Add(supportMetricsGroup);
    configForm.Controls.Add(relGroup);
    configForm.Controls.Add(okButton);
    configForm.Controls.Add(cancelButton);
    configForm.Controls.Add(helpButton);
}

// --- Helper Functions ---
// Function to get measure prefix
string GetMeasurePrefix(string method)
{
    switch (method)
    {
        case "SUM":
        case "SUMX":
            return PREFIX_SUM;
        case "COUNT":
        case "COUNTX":
            return PREFIX_COUNT;
        case "DISTINCTCOUNT":
            return PREFIX_DISTINCTCOUNT;
        default:
            return method;
    }
}

// Function to get DAX expression
string GetDaxExpression(string method, Column column)
{
    switch (method)
    {
        case "SUM":
            return $"SUM({column.DaxObjectFullName})";
        case "SUMX":
            return $"SUMX('{column.Table.Name}', {column.DaxObjectFullName})";
        case "COUNT":
            return $"COUNT({column.DaxObjectFullName})";
        case "COUNTX":
            return $"COUNTX('{column.Table.Name}', {column.DaxObjectFullName})";
        case "DISTINCTCOUNT":
            return $"DISTINCTCOUNT({column.DaxObjectFullName})";
        default:
            return "";
    }
}

// Function to create supporting measures
void CreateSupportingMeasures(Measure baseMeasure, string baseMeasureName, Column c, string formatString, string method, string dateColumnDax, string parentFolder = null)
{
    // Base display folder
    string displayFolderBase = parentFolder != null ? parentFolder : $"{c.DisplayFolder}\\{baseMeasureName}";

    // Base measure reference
    string baseMeasureReference = $"[{baseMeasureName}]";

    // Check if dateColumnDax is provided
    if (string.IsNullOrEmpty(dateColumnDax))
    {
        Error("Date column is required for time-based metrics.");
        return;
    }

    // Time periods
    var periods = new List<dynamic>();
    if (createPM)
    {
        periods.Add(new { Prefix = "PM", Interval = "MONTH", FolderSuffix = "\\PM", Description = "previous month", ShortName = "MoM" });
    }
    if (createPQ)
    {
        periods.Add(new { Prefix = "PQ", Interval = "QUARTER", FolderSuffix = "\\PQ", Description = "previous quarter", ShortName = "QoQ" });
    }
    if (createPY)
    {
        periods.Add(new { Prefix = "PY", Interval = "YEAR", FolderSuffix = "\\PY", Description = "previous year", ShortName = "YoY" });
    }
    if (createPP)
    {
        periods.Add(new { Prefix = "PP", Interval = "PP", FolderSuffix = "\\PP", Description = "previous period", ShortName = "PoP" });
    }

 // Running Total (RT)
    if (createRT)
    {
        var rtMeasureName = $"RT {baseMeasureName}";
        var rtMeasureExpression = $"VAR __lastvisibledate = MAX({dateColumnDax}) RETURN CALCULATE({baseMeasureReference}, {dateColumnDax} <= __lastvisibledate)";
        var rtMeasure = c.Table.AddMeasure(
            rtMeasureName,
            rtMeasureExpression,
            $"{displayFolderBase}\\RT"
        );
        rtMeasure.FormatString = formatString;
        rtMeasure.Description = $"This measure is the running total of {c.DaxObjectFullName}.";
    }   


    foreach (var period in periods)
    {
        // Period measure
        var periodMeasureName = $"{baseMeasureName} {period.Prefix}";
        var periodPrefix = $"{period.Prefix}";
        
        var periodMeasureExpression = $"CALCULATE({baseMeasureReference}, CALCULATETABLE(DATEADD({dateColumnDax}, -1, {period.Interval})))";        
        if (periodPrefix == "PP")
        {
            periodMeasureExpression = $"VAR __min = MIN({dateColumnDax}) VAR __max = MAX({dateColumnDax}) VAR __days = DATEDIFF(__max, __min, DAY) -1  RETURN CALCULATE ({baseMeasureReference}, DATEADD({dateColumnDax}, __days, DAY))";
        }
        

        var displayFolder = $"{displayFolderBase}{period.FolderSuffix}";

        var periodMeasure = c.Table.AddMeasure(
            periodMeasureName,
            periodMeasureExpression,
            displayFolder
        );
        periodMeasure.FormatString = formatString;
        periodMeasure.Description = $"This measure is the {method} of {c.DaxObjectFullName} for the {period.Description}.";

        // Difference measure (e.g., MoM, QoQ, YoY)
        var diffMeasureName = $"{period.ShortName} {baseMeasureName}";
        var diffMeasureExpression = $@"
VAR _currentp = {baseMeasureReference}
VAR _previous = [{periodMeasureName}]
VAR _result = IF(NOT ISBLANK(_previous) && NOT ISBLANK(_currentp), _currentp - _previous)
RETURN _result".Trim();

        var diffMeasure = c.Table.AddMeasure(
            diffMeasureName,
            diffMeasureExpression,
            displayFolder
        );
        diffMeasure.FormatString = formatString;
        diffMeasure.Description = $"This measure is the difference between current and {period.Description} {method} of {c.DaxObjectFullName}.";

        // Percentage difference measure
        var percentDiffMeasureName = $"{diffMeasureName} %";
        var percentDiffMeasureExpression = $"DIVIDE([{diffMeasureName}], [{periodMeasureName}])";
        var percentDiffMeasure = c.Table.AddMeasure(
            percentDiffMeasureName,
            percentDiffMeasureExpression,
            displayFolder
        );
        percentDiffMeasure.FormatString = "#,##0.0%";
        percentDiffMeasure.Description = $"This measure is the percentage change between current and {period.Description} {method} of {c.DaxObjectFullName}.";

        // Color measures if selected
        if (createMetricsWithColors)
        {
            var colorMeasureName = $"_color {diffMeasureName}";
            var colorMeasureExpression = $"VAR __result = [{diffMeasureName}] RETURN SWITCH(TRUE(), __result > 0, [_color_green], __result < 0, [_color_red], BLANK())";
            var colorMeasure = c.Table.AddMeasure(
                colorMeasureName,
                colorMeasureExpression,
                displayFolder
            );
            colorMeasure.FormatString = "Text";

            var bgColorMeasureName = $"_color_bg {diffMeasureName}";
            var bgColorMeasureExpression = $"VAR __result = [{diffMeasureName}] RETURN SWITCH(TRUE(), __result > 0, [_color_green_bg], __result < 0, [_color_red_bg], BLANK())";
            var bgColorMeasure = c.Table.AddMeasure(
                bgColorMeasureName,
                bgColorMeasureExpression,
                displayFolder
            );
            bgColorMeasure.FormatString = "Text";
        }
    }


}

Info("Measures have been created successfully.");
