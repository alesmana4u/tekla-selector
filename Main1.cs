using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Tekla.Structures.Model;

namespace TeklaSelector
{
    public partial class Main1 : Form
    {
        private HashSet<string> selectedSequences = new HashSet<string>();
        private Model teklaModel;

        public Main1()
        {
            InitializeComponent();
            InitializeTeklaConnection();
        }

        private void InitializeTeklaConnection()
        {
            try
            {
                teklaModel = new Model();
                if (!teklaModel.GetConnectionStatus())
                {
                    MessageBox.Show("Tekla Structures is not running.\nPlease open Tekla and load a project.", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error connecting to Tekla: {ex.Message}", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Handle clicks on SEQ buttons (1-10)
        /// </summary>
        private void BtnSeq_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            if (btn != null)
            {
                string sequenceNum = btn.Text;
                string sequenceKey = $"SEQ-{sequenceNum}";
                
                if (selectedSequences.Contains(sequenceKey))
                {
                    selectedSequences.Remove(sequenceKey);
                    btn.BackColor = System.Drawing.SystemColors.Control;
                }
                else
                {
                    selectedSequences.Add(sequenceKey);
                    btn.BackColor = System.Drawing.Color.LightBlue;
                }

                UpdateListBox();
            }
        }

        /// <summary>
        /// Handle custom sequence input
        /// </summary>
        private void BtnAddCustom_Click(object sender, EventArgs e)
        {
            string input = txtCustomSequence.Text.Trim();
            if (string.IsNullOrEmpty(input))
            {
                MessageBox.Show("Please enter a sequence value.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Parse input - support formats like "SEQ-15" or just "15"
            string[] parts = input.Split(new[] { ',', ';' }, System.StringSplitOptions.RemoveEmptyEntries);
            
            foreach (string part in parts)
            {
                string trimmed = part.Trim();
                string sequenceKey;

                if (trimmed.StartsWith("SEQ-", StringComparison.OrdinalIgnoreCase))
                {
                    sequenceKey = trimmed.ToUpper();
                }
                else
                {
                    sequenceKey = $"SEQ-{trimmed}";
                }

                if (!selectedSequences.Contains(sequenceKey))
                {
                    selectedSequences.Add(sequenceKey);
                }
            }

            txtCustomSequence.Clear();
            UpdateListBox();
        }

        /// <summary>
        /// Update the listbox with selected sequences
        /// </summary>
        private void UpdateListBox()
        {
            listBoxSelected.Items.Clear();
            foreach (var seq in selectedSequences.OrderBy(s => ExtractSequenceNumber(s)))
            {
                listBoxSelected.Items.Add(seq);
            }
            UpdateInfo();
        }

        /// <summary>
        /// Extract numeric value from sequence string for sorting
        /// </summary>
        private int ExtractSequenceNumber(string sequence)
        {
            string numStr = sequence.Replace("SEQ-", "").Replace("seq-", "");
            if (int.TryParse(numStr, out int num))
                return num;
            return int.MaxValue;
        }

        /// <summary>
        /// Remove selected item from listbox
        /// </summary>
        private void BtnRemoveSelected_Click(object sender, EventArgs e)
        {
            if (listBoxSelected.SelectedItem != null)
            {
                string selected = listBoxSelected.SelectedItem.ToString();
                selectedSequences.Remove(selected);
                UpdateListBox();
            }
        }

        /// <summary>
        /// Clear all selections
        /// </summary>
        private void BtnClear_Click(object sender, EventArgs e)
        {
            selectedSequences.Clear();
            listBoxSelected.Items.Clear();
            
            // Reset button colors
            foreach (Control control in groupBoxSequence.Controls)
            {
                if (control is Button btn)
                {
                    btn.BackColor = System.Drawing.SystemColors.Control;
                }
            }

            UpdateInfo();
        }

        /// <summary>
        /// Select steel objects in Tekla based on selected sequences
        /// </summary>
        private void BtnSelect_Click(object sender, EventArgs e)
        {
            if (selectedSequences.Count == 0)
            {
                MessageBox.Show("Please select at least one sequence.", "Selection Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                if (teklaModel == null || !teklaModel.GetConnectionStatus())
                {
                    MessageBox.Show("Tekla Structures is not connected.", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int selectedCount = SelectSteelObjects();
                MessageBox.Show($"Selected {selectedCount} steel object(s) with sequence(s): {string.Join(", ", selectedSequences)}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error selecting objects: {ex.Message}", "Selection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Select steel objects based on phase/sequence
        /// </summary>
        private int SelectSteelObjects()
        {
            try
            {
                // First, deselect all existing selections
                ModelObjectEnumerator allObjects = teklaModel.GetModelObjectSelector().GetAllObjects();
                while (allObjects.MoveNext())
                {
                    ModelObject obj = allObjects.Current;
                    obj.Select(false);
                }

                // Now select objects that match our sequences
                allObjects = teklaModel.GetModelObjectSelector().GetAllObjects();
                int objectsSelected = 0;

                while (allObjects.MoveNext())
                {
                    ModelObject obj = allObjects.Current;
                    string phase = GetPhaseFromObject(obj);

                    if (!string.IsNullOrEmpty(phase) && selectedSequences.Contains(phase))
                    {
                        obj.Select(true);
                        objectsSelected++;
                    }
                }

                return objectsSelected;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to select objects: {ex.Message}");
            }
        }

        /// <summary>
        /// Extract phase/sequence information from model object
        /// Supports multiple property lookup methods
        /// </summary>
        private string GetPhaseFromObject(ModelObject obj)
        {
            try
            {
                // Try to cast to Part first
                Part part = obj as Part;
                if (part != null)
                {
                    // Method 1: Get from user-defined properties with EnumAttributes
                    try
                    {
                        string phaseValue = part.GetUserProperty("PHASE", "");
                        if (!string.IsNullOrEmpty(phaseValue))
                        {
                            if (!phaseValue.StartsWith("SEQ-", StringComparison.OrdinalIgnoreCase))
                                return $"SEQ-{phaseValue}";
                            return phaseValue.ToUpper();
                        }
                    }
                    catch { }

                    // Method 2: Get from report properties
                    try
                    {
                        string phaseValue = part.GetReportProperty("PHASE", "");
                        if (!string.IsNullOrEmpty(phaseValue))
                        {
                            if (!phaseValue.StartsWith("SEQ-", StringComparison.OrdinalIgnoreCase))
                                return $"SEQ-{phaseValue}";
                            return phaseValue.ToUpper();
                        }
                    }
                    catch { }

                    // Method 3: Try accessing Phase directly from part properties
                    try
                    {
                        string phaseValue = part.Phase;
                        if (!string.IsNullOrEmpty(phaseValue))
                        {
                            if (!phaseValue.StartsWith("SEQ-", StringComparison.OrdinalIgnoreCase))
                                return $"SEQ-{phaseValue}";
                            return phaseValue.ToUpper();
                        }
                    }
                    catch { }
                }

                // Try Assembly as well
                Assembly assembly = obj as Assembly;
                if (assembly != null)
                {
                    try
                    {
                        string phaseValue = assembly.Phase;
                        if (!string.IsNullOrEmpty(phaseValue))
                        {
                            if (!phaseValue.StartsWith("SEQ-", StringComparison.OrdinalIgnoreCase))
                                return $"SEQ-{phaseValue}";
                            return phaseValue.ToUpper();
                        }
                    }
                    catch { }
                }
            }
            catch
            {
                // Silent fail - object might not have phase info
            }

            return null;
        }

        /// <summary>
        /// Update info label with count
        /// </summary>
        private void UpdateInfo()
        {
            labelInfo.Text = $"Sequences selected: {selectedSequences.Count}";
        }
    }
}
