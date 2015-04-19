using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SMS_API_Demo
{
    /// <summary> Allow User to Provide Criteria for Deleted Device Retrieval </summary>
    public partial class RetrieveDeletedDevices : Form
    {
        public SMS.SMS_API SMS_API;

        public RetrieveDeletedDevices()
        {
            InitializeComponent();
        }

        /// <summary> Event Handler for Cancel Button </summary>
        /// <param name="sender">object</param>
        /// <param name="e">EventArgs</param>
        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        /// <summary> Event HAndler for Get Deleted Devices Button </summary>
        /// <param name="sender">object</param>
        /// <param name="e">EventArgs</param>
        /// <remarks> Hide form. </remarks>
        private void btnGetDeletedDevices_Click(object sender, EventArgs e)
        {
            this.Hide();
        }

        /// <summary> Caption Prefix Post Validation </summary>
        /// <param name="sender">object</param>
        /// <param name="e">EventArgs</param>
        /// <remarks> Prevent empty string. </remarks>
        private void txtCaptionPrefix_Validated(object sender, EventArgs e)
        {
            // Handle case where user enters and deletes some value - do not want empty string left
            if (this.txtCaptionPrefix.Text.Trim().Length == 0)
                this.txtCaptionPrefix.Text = null;
        }         
    }
}
