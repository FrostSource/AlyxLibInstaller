namespace AlyxLibInstallerForms;

partial class Form1
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
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
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        menuStrip1 = new MenuStrip();
        toolStripMenuItem1 = new ToolStripMenuItem();
        addonsToolStripMenuItem = new ToolStripMenuItem();
        openAddonToolStripMenuItem = new ToolStripMenuItem();
        exitToolStripMenuItem1 = new ToolStripSeparator();
        exitToolStripMenuItem = new ToolStripMenuItem();
        checkBox1 = new CheckBox();
        label3 = new Label();
        label2 = new Label();
        textBox1 = new TextBox();
        splitContainer2 = new SplitContainer();
        hScrollBar1 = new HScrollBar();
        vScrollBar1 = new VScrollBar();
        menuStrip1.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)splitContainer2).BeginInit();
        splitContainer2.Panel1.SuspendLayout();
        splitContainer2.SuspendLayout();
        SuspendLayout();
        // 
        // menuStrip1
        // 
        menuStrip1.Items.AddRange(new ToolStripItem[] { toolStripMenuItem1 });
        menuStrip1.Location = new Point(0, 0);
        menuStrip1.Name = "menuStrip1";
        menuStrip1.Size = new Size(800, 24);
        menuStrip1.TabIndex = 0;
        menuStrip1.Text = "menuStrip1";
        // 
        // toolStripMenuItem1
        // 
        toolStripMenuItem1.DropDownItems.AddRange(new ToolStripItem[] { addonsToolStripMenuItem, openAddonToolStripMenuItem, exitToolStripMenuItem1, exitToolStripMenuItem });
        toolStripMenuItem1.Name = "toolStripMenuItem1";
        toolStripMenuItem1.Size = new Size(37, 20);
        toolStripMenuItem1.Text = "File";
        // 
        // addonsToolStripMenuItem
        // 
        addonsToolStripMenuItem.Name = "addonsToolStripMenuItem";
        addonsToolStripMenuItem.Size = new Size(142, 22);
        addonsToolStripMenuItem.Text = "Addons";
        // 
        // openAddonToolStripMenuItem
        // 
        openAddonToolStripMenuItem.Name = "openAddonToolStripMenuItem";
        openAddonToolStripMenuItem.Size = new Size(142, 22);
        openAddonToolStripMenuItem.Text = "Open Addon";
        // 
        // exitToolStripMenuItem1
        // 
        exitToolStripMenuItem1.Name = "exitToolStripMenuItem1";
        exitToolStripMenuItem1.Size = new Size(139, 6);
        // 
        // exitToolStripMenuItem
        // 
        exitToolStripMenuItem.Name = "exitToolStripMenuItem";
        exitToolStripMenuItem.Size = new Size(142, 22);
        exitToolStripMenuItem.Text = "Exit";
        // 
        // checkBox1
        // 
        checkBox1.AutoSize = true;
        checkBox1.Location = new Point(12, 56);
        checkBox1.Name = "checkBox1";
        checkBox1.Size = new Size(90, 19);
        checkBox1.TabIndex = 1;
        checkBox1.Text = "VScript Base";
        checkBox1.UseVisualStyleBackColor = true;
        // 
        // label3
        // 
        label3.AutoSize = true;
        label3.Location = new Point(31, 78);
        label3.Name = "label3";
        label3.Size = new Size(476, 15);
        label3.TabIndex = 4;
        label3.Text = "All AlyxLib vscript related files, including 'gameinit', 'mod init' and '.vscode' settings files.";
        // 
        // label2
        // 
        label2.AutoSize = true;
        label2.Location = new Point(12, 9);
        label2.Name = "label2";
        label2.Size = new Size(134, 15);
        label2.TabIndex = 3;
        label2.Text = "Mod script folder name:";
        // 
        // textBox1
        // 
        textBox1.AcceptsReturn = true;
        textBox1.AcceptsTab = true;
        textBox1.AllowDrop = true;
        textBox1.CharacterCasing = CharacterCasing.Lower;
        textBox1.Location = new Point(12, 27);
        textBox1.Name = "textBox1";
        textBox1.PlaceholderText = "Script folder name";
        textBox1.Size = new Size(424, 23);
        textBox1.TabIndex = 2;
        // 
        // splitContainer2
        // 
        splitContainer2.Dock = DockStyle.Fill;
        splitContainer2.Location = new Point(0, 24);
        splitContainer2.Name = "splitContainer2";
        // 
        // splitContainer2.Panel1
        // 
        splitContainer2.Panel1.Controls.Add(vScrollBar1);
        splitContainer2.Panel1.Controls.Add(hScrollBar1);
        splitContainer2.Panel1.Controls.Add(label2);
        splitContainer2.Panel1.Controls.Add(label3);
        splitContainer2.Panel1.Controls.Add(textBox1);
        splitContainer2.Panel1.Controls.Add(checkBox1);
        splitContainer2.Size = new Size(800, 426);
        splitContainer2.SplitterDistance = 549;
        splitContainer2.TabIndex = 3;
        // 
        // hScrollBar1
        // 
        hScrollBar1.Location = new Point(195, 255);
        hScrollBar1.Name = "hScrollBar1";
        hScrollBar1.Size = new Size(155, 74);
        hScrollBar1.TabIndex = 5;
        // 
        // vScrollBar1
        // 
        vScrollBar1.Location = new Point(510, 13);
        vScrollBar1.Name = "vScrollBar1";
        vScrollBar1.Size = new Size(19, 404);
        vScrollBar1.TabIndex = 6;
        // 
        // Form1
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(800, 450);
        Controls.Add(splitContainer2);
        Controls.Add(menuStrip1);
        MainMenuStrip = menuStrip1;
        Name = "Form1";
        Text = "Form1";
        menuStrip1.ResumeLayout(false);
        menuStrip1.PerformLayout();
        splitContainer2.Panel1.ResumeLayout(false);
        splitContainer2.Panel1.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)splitContainer2).EndInit();
        splitContainer2.ResumeLayout(false);
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private MenuStrip menuStrip1;
    private ToolStripMenuItem toolStripMenuItem1;
    private ToolStripMenuItem addonsToolStripMenuItem;
    private ToolStripMenuItem openAddonToolStripMenuItem;
    private ToolStripSeparator exitToolStripMenuItem1;
    private ToolStripMenuItem exitToolStripMenuItem;
    private CheckBox checkBox1;
    private TextBox textBox1;
    private Label label2;
    private Label label3;
    private SplitContainer splitContainer2;
    private VScrollBar vScrollBar1;
    private HScrollBar hScrollBar1;
}
