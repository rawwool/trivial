namespace Travails
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.treeView1 = new System.Windows.Forms.TreeView();
            this.contextMenuStripTree = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.addFolderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addDocumentToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openFolderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.reloadDiariesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.extractWorklogToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.hoursPerDayToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.hoursPerPerDayPertrackToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.advancedTextEditor1 = new TextRuler.AdvancedTextEditorControl.AdvancedTextEditor();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripSplitButton1 = new System.Windows.Forms.ToolStripSplitButton();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.todayToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripProgressBar1 = new System.Windows.Forms.ToolStripProgressBar();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripProgressBar2 = new System.Windows.Forms.ToolStripProgressBar();
            this.toolStripStatusLabel2 = new System.Windows.Forms.ToolStripStatusLabel();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.contextMenuStripTree.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.treeView1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.advancedTextEditor1);
            this.splitContainer1.Size = new System.Drawing.Size(984, 541);
            this.splitContainer1.SplitterDistance = 192;
            this.splitContainer1.TabIndex = 0;
            // 
            // treeView1
            // 
            this.treeView1.ContextMenuStrip = this.contextMenuStripTree;
            this.treeView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeView1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.treeView1.HideSelection = false;
            this.treeView1.Location = new System.Drawing.Point(0, 0);
            this.treeView1.Name = "treeView1";
            this.treeView1.Size = new System.Drawing.Size(192, 541);
            this.treeView1.TabIndex = 0;
            this.treeView1.AfterLabelEdit += new System.Windows.Forms.NodeLabelEditEventHandler(this.treeView1_AfterLabelEdit);
            this.treeView1.NodeMouseHover += new System.Windows.Forms.TreeNodeMouseHoverEventHandler(this.treeView1_NodeMouseHover);
            this.treeView1.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.treeView1_MouseDoubleClick);
            // 
            // contextMenuStripTree
            // 
            this.contextMenuStripTree.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.addFolderToolStripMenuItem,
            this.addDocumentToolStripMenuItem,
            this.openFolderToolStripMenuItem,
            this.reloadDiariesToolStripMenuItem,
            this.extractWorklogToolStripMenuItem});
            this.contextMenuStripTree.Name = "contextMenuStripTree";
            this.contextMenuStripTree.Size = new System.Drawing.Size(158, 114);
            this.contextMenuStripTree.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenuStripTree_Opening);
            // 
            // addFolderToolStripMenuItem
            // 
            this.addFolderToolStripMenuItem.Name = "addFolderToolStripMenuItem";
            this.addFolderToolStripMenuItem.Size = new System.Drawing.Size(157, 22);
            this.addFolderToolStripMenuItem.Text = "Add &Folder";
            this.addFolderToolStripMenuItem.Click += new System.EventHandler(this.addFolderToolStripMenuItem_Click);
            // 
            // addDocumentToolStripMenuItem
            // 
            this.addDocumentToolStripMenuItem.Name = "addDocumentToolStripMenuItem";
            this.addDocumentToolStripMenuItem.Size = new System.Drawing.Size(157, 22);
            this.addDocumentToolStripMenuItem.Text = "Add &Document";
            this.addDocumentToolStripMenuItem.Click += new System.EventHandler(this.addDocumentToolStripMenuItem_Click);
            // 
            // openFolderToolStripMenuItem
            // 
            this.openFolderToolStripMenuItem.Name = "openFolderToolStripMenuItem";
            this.openFolderToolStripMenuItem.Size = new System.Drawing.Size(157, 22);
            this.openFolderToolStripMenuItem.Text = "&Open Folder";
            this.openFolderToolStripMenuItem.Click += new System.EventHandler(this.openFolderToolStripMenuItem_Click);
            // 
            // reloadDiariesToolStripMenuItem
            // 
            this.reloadDiariesToolStripMenuItem.Name = "reloadDiariesToolStripMenuItem";
            this.reloadDiariesToolStripMenuItem.Size = new System.Drawing.Size(157, 22);
            this.reloadDiariesToolStripMenuItem.Text = "&Reload Diaries";
            this.reloadDiariesToolStripMenuItem.Click += new System.EventHandler(this.reloadDiariesToolStripMenuItem_Click);
            // 
            // extractWorklogToolStripMenuItem
            // 
            this.extractWorklogToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.hoursPerDayToolStripMenuItem,
            this.hoursPerPerDayPertrackToolStripMenuItem});
            this.extractWorklogToolStripMenuItem.Name = "extractWorklogToolStripMenuItem";
            this.extractWorklogToolStripMenuItem.Size = new System.Drawing.Size(157, 22);
            this.extractWorklogToolStripMenuItem.Text = "&Extract Worklog";
            this.extractWorklogToolStripMenuItem.Click += new System.EventHandler(this.extractWorklogToolStripMenuItem_Click);
            // 
            // hoursPerDayToolStripMenuItem
            // 
            this.hoursPerDayToolStripMenuItem.Name = "hoursPerDayToolStripMenuItem";
            this.hoursPerDayToolStripMenuItem.Size = new System.Drawing.Size(217, 22);
            this.hoursPerDayToolStripMenuItem.Text = "&Hours per day";
            this.hoursPerDayToolStripMenuItem.Click += new System.EventHandler(this.hoursPerDayToolStripMenuItem_Click);
            // 
            // hoursPerPerDayPertrackToolStripMenuItem
            // 
            this.hoursPerPerDayPertrackToolStripMenuItem.Name = "hoursPerPerDayPertrackToolStripMenuItem";
            this.hoursPerPerDayPertrackToolStripMenuItem.Size = new System.Drawing.Size(217, 22);
            this.hoursPerPerDayPertrackToolStripMenuItem.Text = "Hours per per day per &track";
            this.hoursPerPerDayPertrackToolStripMenuItem.Click += new System.EventHandler(this.hoursPerPerDayPertrackToolStripMenuItem_Click);
            // 
            // advancedTextEditor1
            // 
            this.advancedTextEditor1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.advancedTextEditor1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.advancedTextEditor1.Location = new System.Drawing.Point(0, 0);
            this.advancedTextEditor1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.advancedTextEditor1.Name = "advancedTextEditor1";
            this.advancedTextEditor1.Size = new System.Drawing.Size(788, 541);
            this.advancedTextEditor1.TabIndex = 0;
            this.advancedTextEditor1.Load += new System.EventHandler(this.advancedTextEditor1_Load);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripSplitButton1,
            this.toolStripProgressBar1,
            this.toolStripStatusLabel1,
            this.toolStripProgressBar2,
            this.toolStripStatusLabel2});
            this.statusStrip1.Location = new System.Drawing.Point(0, 540);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(984, 22);
            this.statusStrip1.TabIndex = 1;
            this.statusStrip1.Text = "statusStrip1";
            this.statusStrip1.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.statusStrip1_ItemClicked);
            // 
            // toolStripSplitButton1
            // 
            this.toolStripSplitButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripSplitButton1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem1,
            this.todayToolStripMenuItem});
            this.toolStripSplitButton1.Image = ((System.Drawing.Image)(resources.GetObject("toolStripSplitButton1.Image")));
            this.toolStripSplitButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripSplitButton1.Name = "toolStripSplitButton1";
            this.toolStripSplitButton1.Size = new System.Drawing.Size(32, 20);
            this.toolStripSplitButton1.Text = "toolStripSplitButton1";
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(107, 22);
            this.toolStripMenuItem1.Text = "Test";
            this.toolStripMenuItem1.Click += new System.EventHandler(this.toolStripMenuItem1_Click);
            // 
            // todayToolStripMenuItem
            // 
            this.todayToolStripMenuItem.Checked = true;
            this.todayToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.todayToolStripMenuItem.Name = "todayToolStripMenuItem";
            this.todayToolStripMenuItem.Size = new System.Drawing.Size(107, 22);
            this.todayToolStripMenuItem.Text = "Today";
            this.todayToolStripMenuItem.Click += new System.EventHandler(this.todayToolStripMenuItem_Click);
            // 
            // toolStripProgressBar1
            // 
            this.toolStripProgressBar1.Name = "toolStripProgressBar1";
            this.toolStripProgressBar1.Size = new System.Drawing.Size(100, 16);
            this.toolStripProgressBar1.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(0, 17);
            // 
            // toolStripProgressBar2
            // 
            this.toolStripProgressBar2.Name = "toolStripProgressBar2";
            this.toolStripProgressBar2.Size = new System.Drawing.Size(100, 16);
            // 
            // toolStripStatusLabel2
            // 
            this.toolStripStatusLabel2.Name = "toolStripStatusLabel2";
            this.toolStripStatusLabel2.Size = new System.Drawing.Size(118, 17);
            this.toolStripStatusLabel2.Text = "toolStripStatusLabel2";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(984, 562);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.splitContainer1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Form1";
            this.Text = "Trivial";
            this.Activated += new System.EventHandler(this.Form1_Activated);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.Form1_KeyUp);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.contextMenuStripTree.ResumeLayout(false);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.TreeView treeView1;
        private TextRuler.AdvancedTextEditorControl.AdvancedTextEditor advancedTextEditor1;
        private System.Windows.Forms.ContextMenuStrip contextMenuStripTree;
        private System.Windows.Forms.ToolStripMenuItem addFolderToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem addDocumentToolStripMenuItem;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripSplitButton toolStripSplitButton1;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem openFolderToolStripMenuItem;
        private System.Windows.Forms.ToolStripProgressBar toolStripProgressBar1;
        private System.Windows.Forms.ToolStripMenuItem todayToolStripMenuItem;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.ToolStripMenuItem reloadDiariesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem extractWorklogToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem hoursPerDayToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem hoursPerPerDayPertrackToolStripMenuItem;
        private System.Windows.Forms.ToolStripProgressBar toolStripProgressBar2;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel2;
    }
}

