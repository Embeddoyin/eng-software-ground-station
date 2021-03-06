﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Reflection;
using System.Windows.Documents;
using System.Runtime.InteropServices;



namespace rLoop_Ground_Station
{
    public partial class Form1 : Form
    {
        // Grid settings
        //------------------------------------------------
        int GRID_ROWS = 18;
        int GRID_CELL_COLUMNS = 6;
        //------------------------------------------------

        // strings for displaying the battery grid values
        String stateCellNegativeTemperature;
        String stateCellPositiveTemperature;
        String stateRowDischarge;
        String stateRowVoltage;

        Label[,] lblCellRowsTempPositiveTabs;
        Label[,] lblCellRowsTempNegativeTabs;
        Label[] lblCellRowsTransistors;
        Label[] lblCellRowsVoltages;

        public Form1()
        {
            InitializeComponent();

            // arrays to store all the labels in the grid
            lblCellRowsTempPositiveTabs = new Label[18, 6];
            lblCellRowsTempNegativeTabs = new Label[18, 6];
            lblCellRowsTransistors = new Label[18];
            lblCellRowsVoltages = new Label[18];
            if (!rPodNodeDiscovery.beginUDPListen())
            {
                MessageBox.Show("There was an error listening to the network for available nodes.");
            }

            generateBatteryTable();

            // listen for tab navigation
            customTabControl1.Selected += new TabControlEventHandler(customTabControl1_SelectedIndexChanged);
        }

        private void customTabControl1_SelectedIndexChanged(Object sender, EventArgs e)
        {
            TabPage current = (sender as TabControl).SelectedTab;
            switch (current.Name)
            {
                case "OverviewTab":
                    UpdateNodeList.Enabled = false;
                    UpdateDGVTimer.Enabled = false;
                    BatteryPackAStatusTab.Enabled = false;
                    break;
                case "PowerNodeATab":
                    UpdateNodeList.Enabled = false;
                    UpdateDGVTimer.Enabled = false;
                    BatteryPackAStatusTab.Enabled = true;
                    break;
                case "NodeUtilitiesTab":
                    UpdateNodeList.Enabled = true;
                    UpdateDGVTimer.Enabled = true;
                    BatteryPackAStatusTab.Enabled = false;
                    break;
                default:
                    UpdateNodeList.Enabled = false;
                    UpdateDGVTimer.Enabled = false;
                    BatteryPackAStatusTab.Enabled = false;
                    break;
            }
        }


        private void generateBatteryTable()
        {
            FlowLayoutPanel headerRow = new   FlowLayoutPanel();
            headerRow.AutoSize = true;
            headerRow.FlowDirection = FlowDirection.LeftToRight;
            string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            FlowLayoutPanel row;
            Label label;
            bool isOdd;

            // create the header labels
            for (int x = 0; x < GRID_CELL_COLUMNS; x++)
            {
                isOdd = (x % 2) == 1;
                // add top labels
                Label labelTop = new Label();
                labelTop.Text = alphabet[x].ToString();
                labelTop.Margin = new Padding(40, 0, 23, 0);
                labelTop.Location = new System.Drawing.Point( 50 * x , 15);
                labelTop.Size = new System.Drawing.Size(30, 25);
                labelTop.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                labelTop.ForeColor = System.Drawing.SystemColors.ButtonFace;
                labelTop.Name = "header" + x;
                headerRow.Controls.Add(labelTop);
            }
            this.flowLayoutPanel1.Controls.Add(headerRow);

            // rows
            for (int y = 0; y < GRID_ROWS; y++)
            {                
                // create row
                row = new FlowLayoutPanel();
                //row.BorderStyle = BorderStyle.Fixed3D;
                row.AutoSize = true;
                row.FlowDirection = FlowDirection.LeftToRight;

                // We multiply by two because the interface shows two temperature values per column
                for (int x = 0; x < 2 * GRID_CELL_COLUMNS; x++) 
                {
                    isOdd = (x % 2) == 1;

                    // create label and append to row
                    label = new Label();
                    label.Margin = new Padding(0, 0, 15 * (isOdd?0:1), 0); // add spacing between odd columns. Could be replaced with a better component
                    label.Text = "";
                    label.Location = new System.Drawing.Point( 30 * x, 15 * y );
                    label.Size = new System.Drawing.Size(40, 20);
                    label.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                    label.ForeColor = System.Drawing.SystemColors.ButtonFace;
                    label.Name = "cell" + x + y;

                    // store label to array (temperature negative/positive, voltage, transistor )
                    if (!isOdd)
                    {
                        int ix = ((x / 2) - 1) + 1;
                        lblCellRowsTempNegativeTabs[y, ix] = label;
                    }
                    else
                    {
                        int ix = (int)(((x / 2) - 1) + 1.5);
                        lblCellRowsTempPositiveTabs[y, ix] = label;
                    }

                    // add to interface
                    row.Controls.Add(label);

                    if (x == 11)
                    {
                        int transistorValue = 0;
                        Label transistorLabel = new Label();
                        transistorLabel.Text = transistorValue.ToString();
                        transistorLabel.Location = new System.Drawing.Point(110 * x, 135 * y);
                        transistorLabel.Size = new System.Drawing.Size(65, 15);
                        transistorLabel.ForeColor = System.Drawing.SystemColors.ControlLight;
                        transistorLabel.Name = "transistor" + x + y;
                        lblCellRowsTransistors[y] = transistorLabel;
                        row.Controls.Add(transistorLabel);


                        int rowVoltageValue = 0;
                        Label rowVoltageLabel = new Label();
                        rowVoltageLabel.Text = rowVoltageValue + "V";
                        rowVoltageLabel.Location = new System.Drawing.Point(110 * x, 160 * y);
                        rowVoltageLabel.Size = new System.Drawing.Size(65, 15);
                        rowVoltageLabel.ForeColor = System.Drawing.SystemColors.ControlLight;
                        rowVoltageLabel.Name = "rowVoltage" + x + y;
                        lblCellRowsVoltages[y] = rowVoltageLabel;
                        row.Controls.Add(rowVoltageLabel);
                    }
                }
                this.flowLayoutPanel1.Controls.Add(row);
            }

        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex < 0)
            {
                MessageBox.Show("Choose a node from the list.");
                return;
            }
            if(!File.Exists(openFileDialog1.FileName))
            {
                MessageBox.Show("Choose a valid file.");
                return;
            }
            rPodNetworking.uploadFile(lblSelectedNodeIp.Text, "root", "MoreCowbell", openFileDialog1.FileName, openFileDialog1.SafeFileName);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Maximized;

            //Interesting idea, not quite working well enough yet
            //Console.SetOut(new ControlWriter(txtConsole));

            float DPIsf = this.CreateGraphics().DpiX / 96;
            customTabControl1.Font = new Font(customTabControl1.Font.FontFamily, customTabControl1.Font.Size * DPIsf);
            customTabControl1.Padding = new Point( (int)(customTabControl1.Padding.X * DPIsf), (int)(customTabControl1.Padding.Y * DPIsf));
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            List<rPodNetworkNode> toRemove = new List<rPodNetworkNode>();
            if (rPodNodeDiscovery.ActiveNodes != null)
            {
                foreach (rPodNetworkNode i in rPodNodeDiscovery.ActiveNodes)
                {
                    if (!listBox1.Items.Contains(i))
                        listBox1.Items.Add(i);
                }
                foreach(rPodNetworkNode i in listBox1.Items )
                {
                    if (!rPodNodeDiscovery.ActiveNodes.Contains(i))
                        toRemove.Add(i);
                }
            }
            foreach (rPodNetworkNode i in toRemove)
                listBox1.Items.Remove(i);
        }

        private void updateNodeStats()
        {
            if (listBox1.SelectedIndex >= 0)
            {
                lblSelectedNodeIp.Text = (listBox1.Items[listBox1.SelectedIndex] as rPodNetworkNode).IP;
                if ((listBox1.Items[listBox1.SelectedIndex] as rPodNetworkNode).IsDataLogging)
                    lblSelectedNodeDataLogging.Text = "Data logging on.";
                else
                    lblSelectedNodeDataLogging.Text = "Data logging off.";
                lblSelectedNodeTime.Text = (listBox1.Items[listBox1.SelectedIndex] as rPodNetworkNode).NodeTime.ToString();
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex >= 0)
            {
                updateNodeStats();
                dataGridView1.Rows.Clear();
            }
        }

        private void UpdateDGVTimer_Tick(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex < 0)
            {
                dataGridView1.Rows.Clear();
                return;
            }
            string node = listBox1.Items[listBox1.SelectedIndex].ToString();
            LatestNodeDataNode nodeData = rPodNetworking.LatestNodeData.FirstOrDefault(x => (x.NodeName.Substring(0,1).ToUpper() + x.NodeName.Substring(1) + " Node") == node);
            if (nodeData != null)
            {
                foreach(NodeDataPoint p in nodeData.DataValues)
                {
                    bool found = false;
                    foreach(DataGridViewRow row in dataGridView1.Rows)
                    {
                        if (row.Cells[0].Value != null && row.Cells[0].Value.ToString() == ("0x" + p.Index.ToString("X4")))
                        {
                            row.Cells[1].Value = p.Value.ToString();
                            found = true;
                            break;
                        }
                    }
                    if(!found)
                    {
                        int row = dataGridView1.Rows.Add();
                        dataGridView1.Rows[row].Cells[0].Value = "0x"+ p.Index.ToString("X4");
                        dataGridView1.Rows[row].Cells[1].Value = p.Value;

                        nodeTypes t = rPodNetworking.nodeParameterData.NodeTypes.FirstOrDefault(x => node == (x.Name.Substring(0, 1).ToUpper() + x.Name.Substring(1) + " Node"));
                        if(t != null)
                        {
                            NodeParameterDefinition def = t.ParameterDefs.FirstOrDefault(x => x.Index == p.Index);
                            if (def != null)
                            {
                                dataGridView1.Rows[row].Cells[2].Value = def.Units;
                                dataGridView1.Rows[row].Cells[3].Value = def.Description;
                            }

                        }
                    }
                }
            }
        }


        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            rPodNetworking.IsRunning = false;
            rPodNodeDiscovery.IsRunning = false;
        }

        public float getScalingFactor()
        {

            float thisDPI = this.CreateGraphics().DpiX;
            return thisDPI / 96;
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            try {
                if (Form1.ActiveForm != null)
                {
                    customTabControl1.Size = new Size(Form1.ActiveForm.Width - 28, Form1.ActiveForm.Height - 50);
                }
                customTabControl1.Location = new Point(5, 5);
            }
            catch (Exception formResizeException)
            {
                Console.Write(formResizeException.StackTrace);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if(listBox1.SelectedIndex < 0)
            {
                MessageBox.Show("Choose a node from the list.");
                return;
            }
            rPodNetworking.changeNodeName(lblSelectedNodeIp.Text, "root", "MoreCowbell", textBox1.Text);
        }


        private void sendTestData_Click(object sender, EventArgs e)
        {
            if(listBox1.SelectedIndex < 0)
            {
                MessageBox.Show("Choose a node from the list.");
                return;
            }

            List<DataParameter> paramsToSend = new List<DataParameter>();
            UInt16 index;
            object value = null;
            if (testDataIndexTxt.Text.Length > 2 && testDataIndexTxt.Text.Substring(0, 2) == "0x")
                index = Convert.ToUInt16(testDataIndexTxt.Text.Substring(2), 16);
            else
                UInt16.TryParse(testDataIndexTxt.Text, out index);
            switch(testDataType.SelectedIndex)
            {
                case 0:  sbyte sbyteVal;
                    if(!sbyte.TryParse(testDataToSendTxt.Text, out sbyteVal))
                        goto error;
                    value = sbyteVal;
                    break;
                case 1: byte byteVal;
                    if (!byte.TryParse(testDataToSendTxt.Text, out byteVal))
                        goto error;
                    value = byteVal;
                    break;
                case 2: Int16 int16Val;
                    if (!Int16.TryParse(testDataToSendTxt.Text, out int16Val))
                        goto error;
                    value = int16Val;
                    break;
                case 3: UInt16 uint16Val;
                    if (!UInt16.TryParse(testDataToSendTxt.Text, out uint16Val))
                        goto error;
                    value = uint16Val;
                    break;
                case 4: Int32 Int32Val;
                    if (!Int32.TryParse(testDataToSendTxt.Text, out Int32Val))
                        goto error;
                    value = Int32Val;
                    break;
                case 5: UInt32 UInt32Val;
                    if (!UInt32.TryParse(testDataToSendTxt.Text, out UInt32Val))
                        goto error;
                    value = UInt32Val;
                    break;
                case 6: Int64 Int64Val;
                    if (!Int64.TryParse(testDataToSendTxt.Text, out Int64Val))
                        goto error;
                    value = Int64Val;
                    break;
                case 7: UInt64 UInt64Val;
                    if (!UInt64.TryParse(testDataToSendTxt.Text, out UInt64Val))
                        goto error;
                    value = UInt64Val;
                    break;
                case 8: float floatVal;
                    if (!float.TryParse(testDataToSendTxt.Text, out floatVal))
                        goto error;
                    value = floatVal;
                    break;
                case 9: double doubleVal;
                    if (!double.TryParse(testDataToSendTxt.Text, out doubleVal))
                        goto error;
                    value = doubleVal;
                    break;
            }

            DataParameter p = new DataParameter();
            p.Index = index;
            p.Data = value;

            paramsToSend.Add(p);

            if (!rPodNetworking.setParameters(listBox1.SelectedItem.ToString(), paramsToSend))
                MessageBox.Show("There was an error sending the message.");

            return;

            error:
                MessageBox.Show("Coud not parse one of the fields.");
        }



        private void BatteryPackAStatusTab_Tick(object sender, EventArgs e)
        {
            if (rPodPodState.PowerNodeA == null)
                return;

            BrakesAPackVoltage.Text = "Voltage: " + rPodPodState.PowerNodeA.BatteryPackVoltage.ToString() + "V";
            BrakesAPackTemperature.Text = "Temperature: " + rPodPodState.PowerNodeA.BatteryPackTemperature.ToString() + "°C";

            for (int y = 0; y < GRID_ROWS; y++)
            {
                for (int x = 0; x < GRID_CELL_COLUMNS; x++)
                {
                    stateCellNegativeTemperature = rPodPodState.PowerNodeA.CellNegativeTabTemperature[y, x].ToString() + "°C";
                    stateCellPositiveTemperature = rPodPodState.PowerNodeA.CellPositiveTabTemperature[y, x].ToString() + "°C";
                    if (lblCellRowsTempNegativeTabs[y, x].Text != stateCellNegativeTemperature)
                        lblCellRowsTempNegativeTabs[y, x].Text = stateCellNegativeTemperature;

                    if (lblCellRowsTempPositiveTabs[y, x].Text != stateCellPositiveTemperature)
                        lblCellRowsTempPositiveTabs[y, x].Text = stateCellPositiveTemperature;


                    //totalRowVoltage += rPodPodState.PowerNodeA.CellVoltages[y, x];
                    //totalRowVoltage = rPodPodState.PowerNodeA.RowVoltage[y];
                    //Console.WriteLine(rPodPodState.PowerNodeA.CellVoltages[0, 0] + " " + rPodPodState.PowerNodeA.CellVoltages[0, 1]);
                }
                stateRowDischarge = rPodPodState.PowerNodeA.BatteryRowDischarging[y].ToString();
                if (lblCellRowsTransistors[y].Text != stateRowDischarge)
                    lblCellRowsTransistors[y].Text = stateRowDischarge;

                stateRowVoltage = rPodPodState.PowerNodeA.RowVoltage[y] + "V";
                if (lblCellRowsVoltages[y].Text != stateRowVoltage)
                    lblCellRowsVoltages[y].Text = stateRowVoltage;
            }
        }


        private void btnNewBaudRate_Click(object sender, EventArgs e)
        {
            int newBaud;
            if (int.TryParse(txtNewBaud.Text, out newBaud)) {
                if (listBox1.SelectedIndex < 0)
                {
                    MessageBox.Show("Choose a node from the list.");
                    return;
                }
                rPodNetworking.changeBaudrate(lblSelectedNodeIp.Text, "root", "MoreCowbell", txtNewBaud.Text);
            }
            else
            {
                MessageBox.Show("Please enter a valid integer for the baud rate.");
            }
        }

        private void btnStartDataLogging_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex < 0)
            {
                MessageBox.Show("Choose a node from the list.");
                return;
            }
            rPodNetworking.startNodeDataLogging(lblSelectedNodeIp.Text, "root", "MoreCowbell");
        }

        private void btnStopDataLogging_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex < 0)
            {
                MessageBox.Show("Choose a node from the list.");
                return;
            }
            rPodNetworking.stopNodeDataLogging(lblSelectedNodeIp.Text, "root", "MoreCowbell");
        }

        private void tmrUpdateNodeUtilStats_Tick(object sender, EventArgs e)
        {
            updateNodeStats();
        }

        private void btnSyncPiClk_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex < 0)
            {
                MessageBox.Show("Choose a node from the list.");
                return;
            }
            rPodNetworking.setNodeTime(lblSelectedNodeIp.Text, "root", "MoreCowbell");
        }


        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            //Don't try and process every random key hit on the form's control
            if ((e.Modifiers & Keys.Control) != Keys.Control)
                return;

            //Use CTRL+Digit to change tabs in the window
            if ((Control.ModifierKeys & Keys.Control) == Keys.Control && e.KeyCode == Keys.D1)
                customTabControl1.SelectedIndex = 0;
            if ((Control.ModifierKeys & Keys.Control) == Keys.Control && e.KeyCode == Keys.D2)
                customTabControl1.SelectedIndex = 1;
            if ((Control.ModifierKeys & Keys.Control) == Keys.Control && e.KeyCode == Keys.D3)
                customTabControl1.SelectedIndex = 2;
            if ((Control.ModifierKeys & Keys.Control) == Keys.Control && e.KeyCode == Keys.D4)
                customTabControl1.SelectedIndex = 3;
            if ((Control.ModifierKeys & Keys.Control) == Keys.Control && e.KeyCode == Keys.D5)
                customTabControl1.SelectedIndex = 4;
            if ((Control.ModifierKeys & Keys.Control) == Keys.Control && e.KeyCode == Keys.D6)
                customTabControl1.SelectedIndex = 5;
            if ((Control.ModifierKeys & Keys.Control) == Keys.Control && e.KeyCode == Keys.D7)
                customTabControl1.SelectedIndex = 6;
        }



        private void button1_Click(object sender, EventArgs e)
        {
            List<DataParameter> paramsToSend = new List<DataParameter>();

            DataParameter p = new DataParameter();
            p.Index = 10000;
            p.Data = (UInt64)(0x3141592653589793);

            paramsToSend.Add(p);

            DataParameter p2 = new DataParameter();
            p2.Index = 10001;
            p2.Data = (byte)(1);

            paramsToSend.Add(p2);

            if (!rPodNetworking.setParameters(listBox1.SelectedItem.ToString(), paramsToSend))
                MessageBox.Show("There was an error sending the message.");
        }

        private void powerTabPage1_Load(object sender, EventArgs e)
        {

        }
    }
}
