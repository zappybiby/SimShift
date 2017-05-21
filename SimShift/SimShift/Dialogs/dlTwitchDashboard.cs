using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SimShift.Dialogs
{
    public partial class dlTwitchDashboard : Form
    {
        private ucDashboard dsh;

        public dlTwitchDashboard()
        {
            this.InitializeComponent();
            this.SetStyle(ControlStyles.UserPaint, true);
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);

            var updateUi = new Timer();
            updateUi.Interval = 20;
            updateUi.Tick += new EventHandler(this.updateUi_Tick);
            updateUi.Start();
            this.dsh = new ucDashboard(Color.FromArgb(5, 5, 5));
            this.dsh.Dock = DockStyle.Fill;
            this.Controls.Add(this.dsh);

            this.StartPosition = FormStartPosition.CenterScreen;
        }

        void updateUi_Tick(object sender, EventArgs e)
        {
            this.dsh.Invalidate();
        }
    }
}