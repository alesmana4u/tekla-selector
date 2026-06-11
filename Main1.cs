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
        /// Tekla 2022 compatible version
        /// </summary>
        private int SelectSteelObjects()
        {
            try
            {
                ModelObjectEnumerator allObjects = teklaModel.GetModelObjectSelector().GetAllObjects();
                int objectsSelected = 0;

                // Deselect all first
                ModelObjectEnumerator deselectAll = teklaModel.GetModelObjectSelector().GetAllObjects();
                while (deselectAll.MoveNext())
                {
                    ModelObject obj = deselectAll.Current;
                    // Use Select with no arguments (default behavior in Tekla 2022)
                    obj.Select();
                }

                // Now select matching objects
                allObjects = teklaModel.GetModelObjectSelector().GetAllObjects();
                while (allObjects.MoveNext())
                {
                    ModelObject obj = allObjects.Current;
                    string phase = GetPhaseFromObject(obj);

                    if (!string.IsNullOrEmpty(phase) && selectedSequences.Contains(phase))
                    {
                        obj.Select();
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
        /// Tekla 2022 compatible version
        /// </summary>
        private string GetPhaseFromObject(ModelObject obj)
        {
            try
            {
                // Try to cast to Part
                Part part = obj as Part;
                if (part != null)
                {
                    // Method 1: Get from user-defined properties
                    // Note: In Tekla 2022, GetUserProperty may require ref parameter or specific enum
                    try
                    {
                        object phaseObj = null;
                        // Try using GetDynamicStringProperty
                        if (part.GetDynamicStringProperty("PHASE", ref phaseObj))
                        {
                            string phaseValue = phaseObj as string;
                            if (!string.IsNullOrEmpty(phaseValue))
                            {
                                if (!phaseValue.StartsWith("SEQ-", StringComparison.OrdinalIgnoreCase))
                                    return $"SEQ-{phaseValue}";
                                return phaseValue.ToUpper();
                            }
                        }
                    }
                    catch { }

                    // Method 2: Try GetReportProperty with ref parameter
                    try
                    {
                        object phaseObj = null;
                        if (part.GetReportProperty("PHASE", ref phaseObj))
                        {
                            string phaseValue = phaseObj as string;
                            if (!string.IsNullOrEmpty(phaseValue))
                            {
                                if (!phaseValue.StartsWith("SEQ-", StringComparison.OrdinalIgnoreCase))
                                    return $"SEQ-{phaseValue}";
                                return phaseValue.ToUpper();
                            }
                        }
                    }
                    catch { }
                }

                // Try Assembly
                Assembly assembly = obj as Assembly;
                if (assembly != null)
                {
                    try
                    {
                        object phaseObj = null;
                        if (assembly.GetReportProperty("PHASE", ref phaseObj))
                        {
                            string phaseValue = phaseObj as string;
                            if (!string.IsNullOrEmpty(phaseValue))
                            {
                                if (!phaseValue.StartsWith("SEQ-", StringComparison.OrdinalIgnoreCase))
                                    return $"SEQ-{phaseValue}";
                                return phaseValue.ToUpper();
                            }
                        }
                    }
                    catch { }
                }
            }
            catch
            {
                // Silent fail
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
