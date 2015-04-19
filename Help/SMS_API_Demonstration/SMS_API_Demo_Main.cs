using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;    
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using SMS;
using System.Collections.Generic;
using System.Timers;

// Remove all references to Schlage: 2013-03-28

namespace SMS_API_Demo
{
  public partial class SMS_API_DemoForm : Form, ITransactionQueueAPISupport
  {
    #region Fields
      string MROglobal = "";
      bool _connectedToDatabase = false;
      bool _connectedToSystemProcessor = false;
      string _iniFile = Application.StartupPath + "\\SMS_API.ini"; // Updated 2013-03-28

      /// <summary> _SMS_API field </summary>
      /// <remarks> Stores an instance of the SMS.SMS_API class.  This is the primary class for interaction with the
      /// SMS Access Control System. Only one instance of the SMS_API object can be instantiated
      /// within a process.  Access must be via the thread that created the instance (i.e. instance is NOT thread safe).
      /// Clients should immediately call the Dispose() method and set the reference to null once finished with the object.</remarks>
      SMS.SMS_API _SMS_API;

      /// <summary> _transactionQueue field </summary>
      /// <remarks> Stores an instance of the APITransactionQueue class when the UseBackgroundThread checkbox is checked.
      /// The queue buffers requests made from the GUI. The requests are processed one by one using a separate thread.
      /// Updates to the GUI are synchronized.</remarks>
      APITransactionQueue _transactionQueue;
    #endregion

    #region Delegates
      delegate void AddLogLineCallback(String s);
      delegate void CloseAPICallback();
      delegate void HandleAPIConnectionCallbackAfter(bool connected);
      delegate void HandleAPIConnectionCallbackBefore();
      delegate void HandleAlarmCallback(Alarm alarm);
      delegate void HandleConnectionStatusChangedCallback(bool connectedToSP, bool connectedToDatabase);
      delegate void HandleTransactionCallback(Transaction transaction);
      delegate void PerformAPIProcessingCallback(QueueItem qi);
      delegate void RefreshFormDataCallback();
      delegate void SetEnabledStatesCallback(bool enabled);
    #endregion

    #region Properties
      /// <summary>APITransactionQueue property</summary>
      /// <remarks> Used to access/create an instance of the APITransactionQueue class.
      /// An APITransactionQueue instance is created when the UseBackgroundThread checkbox is checked.</remarks>
      APITransactionQueue TransactionQueue
      {
        get
        {
          // In this demo application, we already create an APITransactionQueue instance when the user clicks
          // the OpenConnectionButton button, but CreateAPITransactionQueue() is included here so that you
          // can modify where CreateAPITransactionQueue() gets its values from before the queue is created
          // (in the real world...).
          CreateAPITransactionQueue();
          return _transactionQueue;
        }
      }
    #endregion

    #region GUI Event Handlers
      /// <summary>Acknowledge Alarm</summary>
      /// <param name="sender">object</param>
      /// <param name="e">EventArgs</param>
      void AcknowlegeAlarmButton_Click(object sender, EventArgs e)
      {
        QueueItem qi = new QueueItem();

        qi.OperatorName = OperatorNameTextBox.Text;
        qi.TransactionKind = QueueItem.Kind.AcknowledgeAlarm;
        qi.TrnHisID = Convert.ToInt32(AlarmNumericUpDown.Value);

        if (UseBackgroundThread.Checked)
          TransactionQueue.EnqueueItem(qi);
        else
        {
          // Use the SMS_API instance created when the OpenConnectionButton button was clicked.
          qi.SMS_API = _SMS_API;

          PerformAPIAcknowledgeAlarmProcessing(qi);
        }
      }

      void ClearLogButton_Click(object sender, EventArgs e)
    {
      // Clear the log memo box.
      LogRichTextBox.Clear();
    }

      /// <summary>Event Handler for Close Connection Button </summary>
      /// <param name="sender">object</param>
      /// <param name="e">EventArgs</param>
      void CloseConnectionButton_Click(object sender, EventArgs e)
      {
        CloseAPI();
      }

      /// <summary>Common Event Handler for Numeric UpDown Control Change</summary>
      /// <param name="sender">object</param>
      /// <param name="e">EventArgs</param>
      void CommonNumericUpDown_ValueChanged(object sender, EventArgs e)
      {
        RefreshFormData();
      }

      /// <summary> Execute MRO </summary>
      /// <param name="sender">object</param>
      /// <param name="e">EventArgs</param>
      void ExecuteMROButton_Click(object sender, EventArgs e)
      {
          Console.Write("One ");
        QueueItem qi = new QueueItem();

        qi.TransactionKind = QueueItem.Kind.ExecuteMRO;
        qi.OperatorName = OperatorNameTextBox.Text;
        //qi.OverrideID = Convert.ToInt32(MRONumericUpDown.Value);
        int x = Int32.Parse(MROglobal);
        qi.OverrideID = Convert.ToInt32(x);
        if (UseBackgroundThread.Checked)
          TransactionQueue.EnqueueItem(qi);
        else
        {
          // Use the SMS_API instance created when the OpenConnectionButton button was clicked.
          qi.SMS_API = _SMS_API;

          PerformAPIExecuteMROProcessing(qi);
        }
       
        //Environment.Exit(0);
        //Console.Write("Two");
      }

      /// <summary> Execute MRO Set </summary>
      /// <param name="sender">object</param>
      /// <param name="e">EventArgs</param>
      void ExecuteMROSetButton_Click(object sender, EventArgs e)
      {
        QueueItem qi = new QueueItem();

        qi.TransactionKind = QueueItem.Kind.ExecuteMROSet;
        qi.OperatorName = OperatorNameTextBox.Text;
        qi.OverrideID = Convert.ToInt32(MROSetNumericUpDown.Value);

        if (UseBackgroundThread.Checked)
          TransactionQueue.EnqueueItem(qi);
        else
        {
          // Use the SMS_API instance created when the OpenConnectionButton button was clicked.
          qi.SMS_API = _SMS_API;

          PerformAPIExecuteMROSetProcessing(qi);
        }
      }

      /// <summary> Retrieve Alarm Comment from DB </summary>
      /// <param name="sender">object</param>
      /// <param name="e">EventArgs</param>
      void GetAlarmCommentsButton_Click(object sender, EventArgs e)
      {
        QueueItem qi = new QueueItem();

        qi.TransactionKind = QueueItem.Kind.GetAlarmComments;
        qi.TrnHisID = Convert.ToInt32(AlarmCommentNumericUpDown.Value);

        if (UseBackgroundThread.Checked)
          TransactionQueue.EnqueueItem(qi);
        else
        {
          // Use the SMS_API instance created when the OpenConnectionButton button was clicked.
          qi.SMS_API = _SMS_API;

          PerformAPIGetAlarmCommentProcessing(qi);
        }
      }

      /// <summary> Retrieve Alarm Criteria from DB </summary>
      /// <param name="sender">object</param>
      /// <param name="e">EventArgs</param>
      /// <remarks>Retrieve Alarm Criteria for Provided Alarm Criteria ID</remarks>      
      void GetAlarmCriteriaButton_Click(object sender, EventArgs e)
      {
        QueueItem qi = new QueueItem();
        qi.TransactionKind = QueueItem.Kind.GetAlarmCriteria;
        qi.AlarmCriteriaID = Convert.ToInt32(AlarmCriteriaNumericUpDown.Value);

        if (UseBackgroundThread.Checked)
          TransactionQueue.EnqueueItem(qi);
        else
        {
          // Use the SMS_API instance created when the OpenConnectionButton button was clicked.
          qi.SMS_API = _SMS_API;

          PerformAPIGetAlarmCriteriaProcessing(qi);
        }
      }

      /// <summary> Retrieve Alarm Instructions from DB </summary>
      /// <param name="sender">object</param>
      /// <param name="e">EventArgs</param>
      void GetAlarmInstructionsButton_Click(object sender, EventArgs e)
      {
        QueueItem qi = new QueueItem();

        qi.TransactionKind = QueueItem.Kind.GetAlarmInsructions;
        qi.AlarmCriteriaID = Convert.ToInt32(AlarmInstructionsNumericUpDown.Value);

        if (UseBackgroundThread.Checked)
          TransactionQueue.EnqueueItem(qi);
        else
        {
          // Use the SMS_API instance created when the OpenConnectionButton button was clicked.
          qi.SMS_API = _SMS_API;

          PerformAPIGetAlarmInstructionProcessing(qi);
        }
      }

      /// <summary>Returns whether an instance of the SMS.SMS_API class exists</summary>
      /// <returns>True, if an instance has been created and stored in the _SMS_API field.  Otherwise, False.</returns>
      bool HasSMS_APIInstance()
      {
        return _SMS_API != null;
      }

      /// <summary>Returns whether an instance of the APITransactionQueue class exists</summary>
      /// <returns>True, if an instance has been created and stored in the _transactionQueue field.  Ohterwise, False.</returns>
      bool HasTransactionQueueInstance()
      {
        return _transactionQueue != null;
      }

      /// <summary>Insert Alarm Comment into DB</summary>
      /// <param name="sender">object</param>
      /// <param name="e">EventArgs</param>      
      void InsertAlarmCommentButton_Click(object sender, EventArgs e)
      {
        QueueItem qi = new QueueItem();

        qi.TransactionKind = QueueItem.Kind.InsertAlarmComment;
        qi.AlarmComment = AlarmCommentRichTextBox.Text.Trim();
        qi.AlarmCommentDateTime = DateTime.Now;
        qi.OperatorName = OperatorNameTextBox.Text.Trim();
        qi.TrnHisID = Convert.ToInt32(AlarmCommentNumericUpDown.Value);

        if (UseBackgroundThread.Checked)
          TransactionQueue.EnqueueItem(qi);
        else
        {
          // Use the SMS_API instance created when the OpenConnectionButton button was clicked.
          qi.SMS_API = _SMS_API;

          PerformAPIInsertAlarmCommentProcessing(qi);
        }
      }

      /// <summary>Open Connections to the SMS SQL Database and System Processor</summary>
      /// <param name="sender">object</param>
      /// <param name="e">EventArgs</param>
      unsafe void OpenConnectionButton_Click(object sender, EventArgs e)
      {
        if (!HasSMS_APIInstance() && !HasTransactionQueueInstance())
        {
          // Disable connection fields on form.
          SetConnectionFieldEnabledStates(false);

          if (UseBackgroundThread.Checked)
          {
            // Create APITransactionQueue instance, and store in _transactionQueue field.
            CreateAPITransactionQueue();
          }
          else
          {
            // Declare and initialize returnCode that's returned by reference (via pointer) from SMS.SMS_API() constructor below.
            int rc = 0;
            int* returnCode = &rc;

            // Create SMS_API instance and open connections to the SMS SQL database and System Processor.
            try
            {
              ToolStripStatusLabel1.Text = "Connecting...";

              // Set "busy" cursor for form and other controls.
              SetControlCursor(Cursors.WaitCursor);

              _SMS_API = new SMS.SMS_API(
                SPHostnameTextBox.Text,
                DBHostnameTextBox.Text,
                DBNameTextBox.Text,
                DBLoginTextBox.Text,
                PasswordTextBox.Text,
                10,
                true,
                "SMS_API_Demo_Main",
                DataDirectoryTextBox.Text,
                returnCode);
            }
            catch (System.Exception ex)
            {
              CloseAPI();

              AddLogLine("Call to create new instance of SMS API raised exception:\n\n" + ex.Message + "\n");

              // Set returnCode to a non-zero value (for failure) for "finally" clause below.
              *returnCode = -999;
            }
            finally
            {
              if (*returnCode == 0)
              {
                _connectedToSystemProcessor = true;
                _connectedToDatabase = true;

                // Enable all form controls that are not connection-related.
                SetNonConnectionFieldEnabledStates(true);

                // Set event handlers.
                _SMS_API.connectionStatusChangedHandler += HandleAPIConnectionStatusChange;
                _SMS_API.transactionHandler += HandleAPITransaction;
                _SMS_API.alarmHandler += HandleAPIAlarm;
                _SMS_API.mROExecutionCompleteHandler += HandleAPIMROExecutionComplete;
                _SMS_API.alarmKillHandler += HandleAPIAlarmKill;
                _SMS_API.alarmAcknowledgementHandler += HandleAPIAlarmAcknowledgement;
                _SMS_API.databaseChangeHandler += HandleAPIDatabaseChange;
                _SMS_API.deviceStatusChangeHandler += HandleAPIDeviceStatusChange;
                _SMS_API.DB.commandTimeoutInSeconds = 600;
              }
              else if (*returnCode != -999)
                AddLogLine("Call to create new instance of SMS API failed with message:\n\n" + _SMS_API.ReturnCodeToString(*returnCode) + "\n");

              // Clear "Connecting..." text.
              ToolStripStatusLabel1.Text = "";

              // Set default cursor for form and other controls.
              SetControlCursor(Cursors.Default);

              RefreshFormData();
            }
          }
        }
        
      }

      /// <summary> Retrieve Area Accesses from DB </summary>
      /// <param name="sender">object</param>
      /// <param name="e">EventArgs</param>
      void ReadAreaAccessesButton_Click(object sender, EventArgs e)
      {
        QueueItem qi = new QueueItem();

        qi.TransactionKind = QueueItem.Kind.ReadAreaAccesses;

        if (UseBackgroundThread.Checked)
          TransactionQueue.EnqueueItem(qi);
        else
        {
          // Use the SMS_API instance created when the OpenConnectionButton button was clicked.
          qi.SMS_API = _SMS_API;

          PerformAPIReadAreaAccessProcessing(qi);
        }
      }

      /// <summary> Retrieve Areas from DB </summary>
      /// <param name="sender">object</param>
      /// <param name="e">EventArgs</param>
      void ReadAreasButton_Click(object sender, EventArgs e)
      {
        QueueItem qi = new QueueItem();

        qi.TransactionKind = QueueItem.Kind.ReadAreas;

        if (UseBackgroundThread.Checked)
          TransactionQueue.EnqueueItem(qi);
        else
        {
          // Use the SMS_API instance created when the OpenConnectionButton button was clicked.
          qi.SMS_API = _SMS_API;

          PerformAPIReadAreaProcessing(qi);
        }
      }

      /// <summary> Retrieve Area Sets from DB </summary>
      /// <param name="sender">object</param>
      /// <param name="e">EventArgs</param>
      void ReadAreaSetsButton_Click(object sender, EventArgs e)
      {
        QueueItem qi = new QueueItem();

        qi.TransactionKind = QueueItem.Kind.ReadAreaSets;

        if (UseBackgroundThread.Checked)
          TransactionQueue.EnqueueItem(qi);
        else
        {
          // Use the SMS_API instance created when the OpenConnectionButton button was clicked.
          qi.SMS_API = _SMS_API;

          PerformAPIReadAreaSetProcessing(qi);
        }
      }

      /// <summary> Retrieve Enabled Video Camera Controls from DB </summary>
      /// <param name="sender">object</param>
      /// <param name="e">EventArgs</param>
      /// <remarks>Display results to Logging Window</remarks>
      void ReadCamerasButton_Click(object sender, EventArgs e)
      {
        QueueItem qi = new QueueItem();

        qi.DeviceID = null;
        qi.TransactionCodeHi = null;
        qi.TransactionCodeID = null;
        qi.TransactionKind = QueueItem.Kind.ReadCameras;
        qi.TransactionCodeLo = null;
        qi.VideoServerID = null;

        if ((int)VideoServerIDNumericUpDown.Value != -99)
          qi.VideoServerID = (int)VideoServerIDNumericUpDown.Value;

        if ((int)VideoDeviceIDNumericUpDown.Value != -99)
          qi.DeviceID = (int)VideoDeviceIDNumericUpDown.Value;

        if ((int)VideoTransCodeHiNumericUpDown.Value != -99)
          qi.TransactionCodeHi = (int)VideoTransCodeHiNumericUpDown.Value;

        if ((int)VideoTranCodeLoNumericUpDown.Value != -99)
          qi.TransactionCodeLo = (int)VideoTranCodeLoNumericUpDown.Value;

        if ((int)VideoTranCodeIDNumericUpDown.Value != -99)
          qi.TransactionCodeID = (int)VideoTranCodeIDNumericUpDown.Value;

        if (UseBackgroundThread.Checked)
          TransactionQueue.EnqueueItem(qi);
        else
        {
          // Use the SMS_API instance created when the OpenConnectionButton button was clicked.
          qi.SMS_API = _SMS_API;

          PerformAPIReadCameraProcessing(qi);
        }
      }

      /// <summary> Retrieve Cardholders from DB </summary>
      /// <param name="sender">object</param>
      /// <param name="e">EventArgs</param>
      void ReadCardholdersButton_Click(object sender, EventArgs e)
      {
        QueueItem qi = new QueueItem();

        qi.TransactionKind = QueueItem.Kind.ReadCardholders;

        if (LastNameTextBox.Text != "")
          qi.FirstName = FirstNameTextBox.Text;
        else
          qi.FirstName = null;

        if (LastNameTextBox.Text != "")
          qi.LastName = LastNameTextBox.Text;
        else
          qi.LastName = null;

        if (UseBackgroundThread.Checked)
          TransactionQueue.EnqueueItem(qi);
        else
        {
          // Use the SMS_API instance created when the OpenConnectionButton button was clicked.
          qi.SMS_API = _SMS_API;

          PerformAPIReadCardholderProcessing(qi);
        }
      }

      /// <summary> Event Handler for Read Deleted Devices Button </summary>
      /// <param name="sender">object</param>
      /// <param name="e">EventArgs</param>
      /// <remarks>Load RetrieveDeletedDevices Form to Select Filtering Options</remarks>
      void ReadDeletedDevicesButton_Click(object sender, EventArgs e)
      {
        SMS_API_Demo.RetrieveDeletedDevices frmDeletedDeviceOptions = new SMS_API_Demo.RetrieveDeletedDevices();

        frmDeletedDeviceOptions.ShowDialog();

        if (frmDeletedDeviceOptions.IsDisposed != true)
        {
          QueueItem qi = new QueueItem();

          qi.AreaID = null;
          qi.CaptionPrefix = "";
          qi.DeviceID = null;
          qi.DeviceTypeID = null;
          qi.EndDateTime = null;
          qi.StartDateTime = null;
          qi.TransactionKind = QueueItem.Kind.ReadDeletedDevices;

          if (frmDeletedDeviceOptions.dtpDateStart.Checked)
          {
            qi.StartDateTime = frmDeletedDeviceOptions.dtpDateStart.Value.Date;

            if (frmDeletedDeviceOptions.dtpDateEnd.Checked)
              qi.EndDateTime = frmDeletedDeviceOptions.dtpDateEnd.Value.Date;
          }

          if ((int)frmDeletedDeviceOptions.numDeviceID.Value != -99)
            qi.DeviceID = (int)frmDeletedDeviceOptions.numDeviceID.Value;

          if ((int)frmDeletedDeviceOptions.numAreaID.Value != -99)
            qi.AreaID = (int)frmDeletedDeviceOptions.numAreaID.Value;

          if ((int)frmDeletedDeviceOptions.numDeviceTypeID.Value != -99)
            qi.DeviceTypeID = (int)frmDeletedDeviceOptions.numDeviceTypeID.Value;

          if (frmDeletedDeviceOptions.txtCaptionPrefix.Text.Length != 0)
            qi.CaptionPrefix = frmDeletedDeviceOptions.txtCaptionPrefix.Text;

          if (UseBackgroundThread.Checked)
            TransactionQueue.EnqueueItem(qi);
          else
          {
            // Use the SMS_API instance created when the OpenConnectionButton button was clicked.
            qi.SMS_API = _SMS_API;

            PerformAPIReadDeletedDeviceProcessing(qi);
          }
        }
      }

      /// <summary> Retrieve Devices from DB </summary>
      /// <param name="sender">object</param>
      /// <param name="e">EventArgs</param>
      void ReadDevicesButton_Click(object sender, EventArgs e)
      {
        QueueItem qi = new QueueItem();

        qi.TransactionKind = QueueItem.Kind.ReadDevices;

        if (UseBackgroundThread.Checked)
          TransactionQueue.EnqueueItem(qi);
        else
        {
          // Use the SMS_API instance created when the OpenConnectionButton button was clicked.
          qi.SMS_API = _SMS_API;

          PerformAPIReadDeviceProcessing(qi);
        }
      }

      /// <summary> Retrieve Device Types from DB </summary>
      /// <param name="sender">object</param>
      /// <param name="e">EventArgs</param>
      void ReadDeviceTypesButton_Click(object sender, EventArgs e)
      {
        QueueItem qi = new QueueItem();

        qi.TransactionKind = QueueItem.Kind.ReadDeviceTypes;

        if (UseBackgroundThread.Checked)
          TransactionQueue.EnqueueItem(qi);
        else
        {
          // Use the SMS_API instance created when the OpenConnectionButton button was clicked.
          qi.SMS_API = _SMS_API;

          PerformAPIReadDeviceTypeProcessing(qi);
        }
      }

      /// <summary> Retrieve MROs from DB </summary>
      /// <param name="sender">object</param>
      /// <param name="e">EventArgs</param>
      void ReadMROsButton_Click(object sender, EventArgs e)
      {
        QueueItem qi = new QueueItem();

        qi.TransactionKind = QueueItem.Kind.ReadMRO;

        if (UseBackgroundThread.Checked)
          TransactionQueue.EnqueueItem(qi);
        else
        {
          // Use the SMS_API instance created when the OpenConnectionButton button was clicked.
          qi.SMS_API = _SMS_API;

          PerformAPIReadMROProcessing(qi);
        }
      }

      /// <summary> Retrieve MRO Sets from DB </summary>
      /// <param name="sender">object</param>
      /// <param name="e">EventArgs</param>
      void ReadMROSetsButton_Click(object sender, EventArgs e)
      {
        QueueItem qi = new QueueItem();
        qi.TransactionKind = QueueItem.Kind.ReadMROSets;

        if (UseBackgroundThread.Checked)
          TransactionQueue.EnqueueItem(qi);
        else
        {
          // Use the SMS_API instance created when the OpenConnectionButton button was clicked.
          qi.SMS_API = _SMS_API;

          PerformAPIReadMROSetProcessing(qi);
        }
      }

      /// <summary> Retrieve Transactions or Alarms from DB </summary>
      /// <param name="sender">object</param>
      /// <param name="e">EventArgs</param>
      void ReadTransactionsButton_Click(object sender, EventArgs e)
      {
        DateTime? startDateTime = null;
        DateTime? endDateTime = null;

        try
        {
          startDateTime = UserStringToDateTime(StartTimeTextBox.Text);
          endDateTime = UserStringToDateTime(EndTimeTextBox.Text);
        }
        catch
        {
          MessageBox.Show("Date Format = yyyy-MM-dd HH:mm:ss.fff Required");
          return;
        }

        QueueItem qi = new QueueItem();

        qi.AreaID = UserIntToInt(Convert.ToInt32(AreaIDNumericUpDown.Value));
        qi.CardholderID = UserIntToInt(Convert.ToInt32(CardholderIDNumericUpDown.Value));
        qi.DeviceID = UserIntToInt(Convert.ToInt32(DeviceIDNumericUpDown.Value));
        qi.StartDateTime = startDateTime;
        qi.EndDateTime = endDateTime;

        if (TransactionsRadioButton.Checked)
        {
          qi.TransactionKind = QueueItem.Kind.ReadTrans;

          if (UseBackgroundThread.Checked)
            TransactionQueue.EnqueueItem(qi);
          else
          {
            // Use the SMS_API instance created when the OpenConnectionButton button was clicked.
            qi.SMS_API = _SMS_API;

            PerformAPIReadTransProcessing(qi);
          }
        }
        else
        {
          qi.TransactionKind = QueueItem.Kind.ReadTransAlarm;

          if (UseBackgroundThread.Checked)
            TransactionQueue.EnqueueItem(qi);
          else
          {
            // Use the SMS_API instance created when the OpenConnectionButton button was clicked.
            qi.SMS_API = _SMS_API;

            PerformAPIReadTransAlarmProcessing(qi);
          }
        }
      }

      /// <summary> Retrieve Transaction Codes from DB </summary>
      /// <param name="sender">object</param>
      /// <param name="e">EventArgs</param>
      void ReadTransCodesButton_Click(object sender, EventArgs e)
      {
        QueueItem qi = new QueueItem();

        qi.TransactionKind = QueueItem.Kind.ReadTransCodes;

        if (UseBackgroundThread.Checked)
          TransactionQueue.EnqueueItem(qi);
        else
        {
          // Use the SMS_API instance created when the OpenConnectionButton button was clicked.
          qi.SMS_API = _SMS_API;

          PerformAPIReadTransCodeProcessing(qi);
        }
      }

      /// <summary> Retrieve Transaction Groups </summary>
      /// <param name="sender">object</param>
      /// <param name="e">EventArgs</param>
      void ReadTransGroupsButton_Click(object sender, EventArgs e)
      {
        QueueItem qi = new QueueItem();
        qi.TransactionKind = QueueItem.Kind.ReadTransGroups;

        if (UseBackgroundThread.Checked)
          TransactionQueue.EnqueueItem(qi);
        else
        {
          // Use the SMS_API instance created when the OpenConnectionButton button was clicked.
          qi.SMS_API = _SMS_API;

          PerformAPIReadTransGroupProcessing(qi);
        }
      }

      /// <summary> Retrieve Enabled Video Servers from DB </summary>
      /// <param name="sender">object</param>
      /// <param name="e">EventArgs</param>
      /// <remarks>Display results to Logging Window</remarks>
      void ReadVideoServersButton_Click(object sender, EventArgs e)
      {
        QueueItem qi = new QueueItem();

        qi.TransactionKind = QueueItem.Kind.ReadVideoServers;

        if (UseBackgroundThread.Checked)
          TransactionQueue.EnqueueItem(qi);
        else
        {
          // Use the SMS_API instance created when the OpenConnectionButton button was clicked.
          qi.SMS_API = _SMS_API;

          PerformAPIReadVideoServerProcessing(qi);
        }
      }

      /// <summary> Request Contact Status </summary>
      /// <param name="sender">object</param>
      /// <param name="e">EventArgs</param>
      void RequestDeviceStatusButton_Click(object sender, EventArgs e)
      {
        QueueItem qi = new QueueItem();

        qi.TransactionKind = QueueItem.Kind.RequestDeviceStatus;
        qi.DeviceID = Convert.ToInt32(StatusNumericUpDown.Value);

        if (UseBackgroundThread.Checked)
          TransactionQueue.EnqueueItem(qi);
        else
        {
          // Use the SMS_API instance created when the OpenConnectionButton button was clicked.
          qi.SMS_API = _SMS_API;

          PerformAPIDeviceStatusProcessing(qi);
        }
      }

      /// <summary> Reset anti-passback State </summary>
      /// <param name="sender">object</param>
      /// <param name="e">EventArgs</param>
      void ResetAPButton_Click(object sender, EventArgs e)
      {
        QueueItem qi = new QueueItem();

        qi.TransactionKind = QueueItem.Kind.ResetAntiPassback;
        qi.EncodedID = Convert.ToUInt32(APEncodedIDNumericUpDown.Value);

        if (UseBackgroundThread.Checked)
          TransactionQueue.EnqueueItem(qi);
        else
        {
          // Use the SMS_API instance created when the OpenConnectionButton button was clicked.
          qi.SMS_API = _SMS_API;

          PerformAPIResetAntiPassbackProcessing(qi);
        }
      }

      /// <summary> Validate and Load Cardholder Portrait </summary>
      /// <param name="sender">object</param>
      /// <param name="e">EventArgs</param>
      void ValidateCardholderPortraitButton_Click(object sender, EventArgs e)
      {
        QueueItem qi = new QueueItem();

        qi.TransactionKind = QueueItem.Kind.ProcessPortrait;
        qi.CardholderID = Convert.ToInt32(CardholderPortraitNumericUpDown.Value);

        if (UseBackgroundThread.Checked)
          TransactionQueue.EnqueueItem(qi);
        else
        {
          // Use the SMS_API instance created when the OpenConnectionButton button was clicked.
          qi.SMS_API = _SMS_API;

          PerformAPIPortraitProcessing(qi);
        }
      }
    #endregion

    /// <summary> Prepend Text to Log Box </summary>
    /// <param name="s">Message</param>
    void AddLogLine(String s)
    {
      if (UseBackgroundThread.InvokeRequired)
      {
        AddLogLineCallback callback = new AddLogLineCallback(AddLogLine);
        Invoke(callback, s);
      }
      else
      {
        LogRichTextBox.Text = s + "\n" + LogRichTextBox.Text;
      }
    }

    ///<summary>Close API Connections</summary>
    void CloseAPI()
    {
      bool needsClose = (HasSMS_APIInstance() || HasTransactionQueueInstance());

      if (!needsClose)
        return;

      if (UseBackgroundThread.InvokeRequired)
      {
        CloseAPICallback callback = new CloseAPICallback(CloseAPI);
        Invoke(callback);
      }
      else
      {
        if (UseBackgroundThread.Checked)
        {
          if (HasTransactionQueueInstance())
            try
            {
              // Notify the queue's background thread to end processing.
              _transactionQueue.EndProcessing();
            }
            catch (System.Exception ex)
            {
              HandleException(ex, "CloseAPI() Exception (ending transaction queue processing):");
            }
        }
        else if (HasSMS_APIInstance())
        {
          try
          {
            // Dispose forces unmanaged resources within the Win32 SPComm.dll to be freed immediately.
            // Otherwise, the resources may not be available if a new SMS_API object is created.
            _SMS_API.Dispose();
          }
          catch (System.Exception ex)
          {
            HandleException(ex, "CloseAPI() Exception (attempting to close SMS API):");
          }
        }

        _SMS_API = null;
        _transactionQueue = null;

        _connectedToSystemProcessor = false;
        _connectedToDatabase = false;

        RefreshFormData();

        SetNonConnectionFieldEnabledStates(false);
        SetConnectionFieldEnabledStates(true);
      }
    }

    /// <summary>Used to create a single instance of the APITransactionQueue class, and store it
    /// in the _transactionQueue field.</summary>
    void CreateAPITransactionQueue()
    {
      if (UseBackgroundThread.Checked && !HasTransactionQueueInstance())
      {
        ConnectionInfo connectionInfo = new ConnectionInfo();

        // Seed structure of connection-related values.
        connectionInfo.DataDirectory = DataDirectoryTextBox.Text;
        connectionInfo.DBHostname = DBHostnameTextBox.Text;
        connectionInfo.DBLogin = DBLoginTextBox.Text;
        connectionInfo.DBName = DBNameTextBox.Text;
        connectionInfo.DBPassword = PasswordTextBox.Text;
        connectionInfo.DBReconnectInterval = 10;
        connectionInfo.EventLogSource = "SMS API Implementation Demo";
        connectionInfo.SPHostname = SPHostnameTextBox.Text;
        connectionInfo.UseVerboseDebugLogging = true;

        #region Create APITransactionQueue instance
        // Queue is used to buffer entries made from the GUI when the UseBackgroundThread
        // checkbox is checked.  Entries are processed one by one (FIFO), with a method
        // of the ITransactionQueueCallSupport interface being called for each.
        // ITransactionQueueCallSupport specifies a method for each SMS_API feature.
        // In this demo application, the PerformAPIXXXProcessing() methods of the
        // SMA_API_Demo_Main class implement the ITransactionQueueCallSupport
        // interface.  The PerformAPIXXXProcessing() methods invoke the SMS_API directly.
        #endregion
        _transactionQueue = new APITransactionQueue(this, connectionInfo);
      }
    }

    /// <summary> Retrieve Credential EncodedIDs from DB </summary>
    /// <param name="credentials">List of Credential Objects</param>
    /// <returns>Delimited String Containing EncodedIDs</returns>
    string GetCredentialsAsString(List<Credential> credentials)
    {
      String result = "";

      foreach (Credential cred in credentials)
      {
        result += "'" + cred.encodedID + "';";
      }

      return result;
    }

    /// <summary>Retrieve UDFs from DB</summary>
    /// <param name="udfs">Hash Table Containing UDF Fields/Values</param>
    /// <returns>Delimited String Containing UDF Fieldnames and Values</returns>
    string GetUDFsAsString(Hashtable udfs)
    {
      String result = "";

      foreach (DictionaryEntry de in udfs)
      {
        if (((string) de.Value) != "")
          result += de.Key + ": '" + de.Value + "'; ";
      }

      return result;
    }

    /// <summary>Perform processing after connection attempt to the SMS_API & database from APITransactionQueue</summary>
    /// <param name="connected"></param>Indicates whether connection was successfull
    public void HandleAPIAfterConnection(bool connected)
    {
      if (!HasTransactionQueueInstance())
        return;

      if (UseBackgroundThread.InvokeRequired)
      {
        HandleAPIConnectionCallbackAfter callback = new HandleAPIConnectionCallbackAfter(HandleAPIAfterConnection);
        Invoke(callback, connected);
      }
      else
      {
        // Clear "Connecting..." text.
        ToolStripStatusLabel1.Text = "";

        // Set default cursor for form and other controls.
        SetControlCursor(Cursors.Default);

        // If we're successfully connected...
        if (connected)
        {
          _connectedToDatabase = true;
          _connectedToSystemProcessor = true;
        }

        // Enable all form controls that are not connection-related.
        SetNonConnectionFieldEnabledStates(true);

        RefreshFormData();
      }
    }

    /// <summary>Handle Real-Time Alarm from System Processor</summary>
    /// <param name="alarm">Current Alarm Object</param>
    public void HandleAPIAlarm(Alarm alarm)
    {
      if (UseBackgroundThread.InvokeRequired)
      {
        HandleAlarmCallback callback = new HandleAlarmCallback(HandleAPIAlarm);
        Invoke(callback, alarm);
      }
      else if (LookupCheckBox.Checked)
      {
        alarm.Initialize();

        AddLogLine("Received Alarm " + alarm.debugString + "\n  --for device: " + alarm.device.caption
          + " with acknowledger name: " + alarm.acknowledgerName);
      }
      else
        AddLogLine("Received Alarm " + alarm.debugString);
    }

    /// <summary>Handle Alarm Killed by System Processor</summary>
    /// <param name="trnHisID">Alarm Transaction ID</param>
    public void HandleAPIAlarmKill(int trnHisID)
    {
      AddLogLine("SP has killed alarm for " + trnHisID);
    }

    /// <summary>Handle Alarm Acknowledgement Confirmation from System Processor</summary>
    /// <param name="trnHisID">Alarm Transaction ID</param>
    /// <param name="acknowledger">Operator Object</param>
    /// <param name="acknowledgedDateTime">Acknowledgement Timestamp</param>
    public void HandleAPIAlarmAcknowledgement(int trnHisID, Operator acknowledger, DateTime acknowledgedDateTime)
    {
      AddLogLine("Alarm Acknowledge message for " + trnHisID + " arrived. Acknowledged by " + acknowledger + " at " + acknowledgedDateTime);
    }

    /// <summary>Perform processing before connection attempt to the SMS_API & database from APITransactionQueue</summary>
    public void HandleAPIBeforeConnection()
    {
      if (UseBackgroundThread.InvokeRequired)
      {
        HandleAPIConnectionCallbackBefore callback = new HandleAPIConnectionCallbackBefore(HandleAPIBeforeConnection);
        Invoke(callback);
      }
      else
      {
        ToolStripStatusLabel1.Text = "Connecting...";

        // Set "busy" cursor for form and other controls.
        SetControlCursor(Cursors.WaitCursor);
      }
    }

    /// <summary>Handle System Processor or DB Connection Change</summary>
    /// <param name="connectedToSP">System Processor Connected</param>
    /// <param name="connectedToDatabase">DB Connected</param>
    public void HandleAPIConnectionStatusChange(bool connectedToSP, bool connectedToDatabase)
    {
      if (UseBackgroundThread.InvokeRequired)
      {
        HandleConnectionStatusChangedCallback callback = new HandleConnectionStatusChangedCallback(HandleAPIConnectionStatusChange);
        Invoke(callback, connectedToSP, connectedToDatabase);
      }
      else
      {
        _connectedToSystemProcessor = connectedToSP;
        _connectedToDatabase = connectedToDatabase;

        RefreshFormData();
      }
    }

    /// <summary>Handle Database Change Notification from the System Processor</summary>
    /// <param name="changedDatabaseTablesBitmap">Changed Tables</param>
    public void HandleAPIDatabaseChange(uint changedDatabaseTablesBitmap)
    {
      AddLogLine("Received DB Change Notice");
    }

    /// <summary>Handle Device Status Change Message from the System Processor</summary>
    /// <param name="deviceStatusMessage">Device Status Message</param>
    public void HandleAPIDeviceStatusChange(DeviceStatusMessage deviceStatusMessage)
    {
      String s = "Device status changed " + deviceStatusMessage.debugString;

      if (deviceStatusMessage is ContactStatusMessage)
      {
        ContactStatusMessage contactStatus = (ContactStatusMessage)deviceStatusMessage;

        switch (contactStatus.status)
        {
          case ContactStatusType.Closed:
            s += "**Contact is closed";
            break;
          case ContactStatusType.Open:
            s += "**Contact is open";
            break;
          case ContactStatusType.SupervisedOpen:
            s += "**Supervision fault detected! Circuit is open";
            break;
          case ContactStatusType.SupervisedShorted:
            s += "**Supervision fault detected! Circuit is closed";
            break;
        }
      }
      else if (deviceStatusMessage is RelayStatusMessage)
      {
        RelayStatusMessage relayStatus = (RelayStatusMessage)deviceStatusMessage;

        switch (relayStatus.status)
        {
          case RelayStatusType.Energized:
            s += "**Relay is energized";
            break;
          case RelayStatusType.Released:
            s += "**Relay is released";
            break;
        }
      }
      else if (deviceStatusMessage is ReaderStatusMessage)
      {
        ReaderStatusMessage readerStatus = (ReaderStatusMessage)deviceStatusMessage;

        if (readerStatus.communicating)
          s = "**Reader is communicating";
        else
          s = "**Reader is not communicating";
      }

      AddLogLine(s);
    }

    /// <summary>Handle Completed MRO Message from System Processor</summary>
    /// <param name="trnHisID">MRO Transaction ID</param>
    /// <param name="statusCode">Status Code</param>
    /// <param name="statusMessage">Status Message</param>
    public void HandleAPIMROExecutionComplete(int trnHisID, int statusCode, string statusMessage)
    {
      AddLogLine("Manual override " + trnHisID + " completed with code " + statusCode + " and message " + statusMessage);
    }

    /// <summary>Handle Real-Time Transaction from System Processor</summary>
    /// <param name="transaction">Current Transaction Object</param>
    public void HandleAPITransaction(Transaction transaction)
    {
      if (UseBackgroundThread.InvokeRequired)
      {
        HandleTransactionCallback callback = new HandleTransactionCallback(HandleAPITransaction);
        Invoke(callback, transaction);
      }
      else if (LookupCheckBox.Checked)
        AddLogLine("Received " + transaction.debugString + "\n  --for device: " + transaction.device.caption);
      else
        AddLogLine("Received " + transaction.debugString);
    }

    /// <summary>Event Handler for generic exceptions</summary>
    /// <param name="ex"></param>Exception
    /// <param name="message"></param>Optional message to prepend to exception message
    public void HandleException(System.Exception ex, string message)
    {
      if (message.ToString().Trim() == "")
        AddLogLine("The following exception occurred:\n\n" + ex.Message + "\n");
      else
        AddLogLine(message + "\n\n" + ex.Message + "\n");
    }

    /// <summary>Event Handler for non-UI thread exceptions</summary>
    public void HandleNonUIException(object sender, UnhandledExceptionEventArgs e)
    {
      if (e.ExceptionObject is SMS.NonFatalException)
      {
        // Application should check the exact type of the non fatal exception
        // (i.e. System Processor, CIM, database, etc) and generate more meaningful message
        AddLogLine("Non-fatal exception occurred.\n\n Detail = " + e.ToString() + "\n");
      }
      else if (e.ExceptionObject is SMS.FatalException)
      {
        // Always close the API after a fatal exception. Might even want to close the application in this case.
        AddLogLine("Fatal exception occurred.\n\n Detail = " + e.ToString() + "\n");
        CloseAPI();
      }
      else
      {
        // Not a known SMS Exception.  Exception may have nothing to do with SMS.
        MessageBox.Show("Unknown exception occurred.\n\n Detail = " + e.ToString());
        CloseAPI();
      }
    }

    /// <summary>Event Handler for APITransactionQueue finalization.
    ///Called when the APITransactionQueue is shutting-down</summary>
    public void HandleTransactionQueueShutdown()
    {
      // Specifically, this call will set the _transactionQueue to null and refresh the form.
      CloseAPI();
    }

    /// <summary>Event Handler for APITransactionQueue initialization.
    ///Called when the APITransactionQueue is starting-up</summary>
    public void HandleTransactionQueueStartup()
    {
      // Nothing to do here for this demo application...
    }

    /// <summary>Event Handler for APITransactionQueue errors</summary>
    /// <param name="errorMessage"></param>Error message
    /// <param name="ex"></param>Exception (optional)
    public void HandleTransactionQueueException(string errorMessage, System.Exception ex)
    {
      AddLogLine(errorMessage);
    }

    /// <summary>Event Handler for UI thread exceptions</summary>
    /// <param name="sender">object</param>
    /// <param name="t">ThreadExceptionEventArgs</param>
    public void HandleUIException(object sender, ThreadExceptionEventArgs e)
    {
      if (e.Exception is SMS.NonFatalException)
      {
        // Application should check the exact type of the non fatal exception
        // (i.e. System Processor, CIM, database, etc) and generate more meaningful message
        AddLogLine("Exception occurred.\n\n Detail = " + e.ToString() + "\n");
      }
      else if (e.Exception is SMS.FatalException)
      {
        // Always close the API after a fatal exception. Might even want to close the application in this case.
        AddLogLine("Exception occurred.\n\n Detail = " + e.ToString() + "\n");
        CloseAPI();
      }
      else
      {
        // Not a known SMS Exception.
        // Exception may have nothing to do with SMS. Exit.
        MessageBox.Show("Exception occurred.\n\nDetail = " + e.ToString());
        CloseAPI();
      }
    }

    /// <summary>Check for SMS_API.ini And Load Connection Settings</summary>
    /// <returns>True if Successful</returns>
    /// <created> 2012-02-08</created>    
    bool LoadINIValues()
    {
      if (File.Exists(_iniFile))
      {
        try
        {
          String[] iniData = File.ReadAllLines(_iniFile);

          foreach (string iniLine in iniData)
          {
            string iniValue = iniLine.Substring(iniLine.IndexOf("=") + 2);

            if ((iniValue != "") & (iniValue != null))
            {
              switch (iniLine.Substring(0, iniLine.IndexOf("=") - 1))
              {
                case "SPHostName":
                  this.SPHostnameTextBox.Text = iniValue;
                  break;
                case "DBHostName":
                  this.DBHostnameTextBox.Text = iniValue;
                  break;
                case "DBName":
                  this.DBNameTextBox.Text = iniValue;
                  break;
                case "DBLogin":
                  this.DBLoginTextBox.Text = iniValue;
                  break;
                case "Password":
                  this.PasswordTextBox.Text = iniValue;
                  break;
                case "DataDirectory":
                  this.DataDirectoryTextBox.Text = iniValue;
                  break;
              }
            }
          }
        }
        catch
        {
          return false;
        }
      }
      return true;
    }

    public void PerformAPIAcknowledgeAlarmProcessing(QueueItem qi)
    {
      try
      {
        qi.SMS_API.AcknowlegeAlarm(qi.TrnHisID, qi.OperatorName);
      }
      catch (System.Exception ex)
      {
        HandleException(ex, "PerformAPIAcknowledgeAlarmProcessing() Exception:");
      }
    }

    public void PerformAPIDeviceStatusProcessing(QueueItem qi)
    {
      try
      {
        qi.SMS_API.RequestStatus((int)qi.DeviceID);
      }
      catch (System.Exception ex)
      {
        HandleException(ex, "PerformAPIDeviceStatusProcessing() Exception:");
      }
    }

    public void PerformAPIExecuteMROProcessing(QueueItem qi)
    {
      try
      {
        qi.SMS_API.ExecuteManualOverrideTask(qi.OverrideID, 0, qi.OperatorName);
      }
      catch (System.Exception ex)
      {
        HandleException(ex, "PerformAPIExecuteMROProcessing() Exception:");
      }
    }

    public void PerformAPIExecuteMROSetProcessing(QueueItem qi)
    {
      try
      {
        qi.SMS_API.ExecuteManualOverrideSet(qi.OverrideID, 0, qi.OperatorName);
      }
      catch (System.Exception ex)
      {
        HandleException(ex, "PerformAPIExecuteMROSetProcessing() Exception:");
      }
    }

    public void PerformAPIGetAlarmCommentProcessing(QueueItem qi)
    {
      try
      {
        string summary = "";

        foreach (AlarmComment ac in qi.SMS_API.DB.GetAlarmComments(qi.TrnHisID))
        {
          summary = summary + "Comment for Alarm #" + ac.trnHisID + " Entered by " + ac.operatorName + " = " + ac.description + " at " + ac.CommentDateTime + "\n";
        }

        if (summary == "")
          summary = "No comments found for Alarm #" + Convert.ToInt32(AlarmCommentNumericUpDown.Value);

        AddLogLine(summary);
      }
      catch (System.Exception ex)
      {
        HandleException(ex, "PerformAPIGetAlarmCommentProcessing() Exception:");
      }
    }

    public void PerformAPIGetAlarmCriteriaProcessing(QueueItem qi)
    {
      try
      {
        AlarmCriteria ac = new AlarmCriteria(qi.SMS_API);

        ac.Initialize(qi.AlarmCriteriaID);

        string criteria = criteria = "Criteria for AlarmCriteriaID # " + ac.alarmCriteriaID + " = " + ac.caption;

        if (ac.description != "")
          criteria += " [" + ac.description + "]"; // Display additional (optional) description if entered

        if (ac.caption == null)
          criteria = "No criteria found for Alarm Criteria ID #" + qi.AlarmCriteriaID;

        AddLogLine(criteria);
      }
      catch (System.Exception ex)
      {
        HandleException(ex, "PerformAPIGetAlarmCriteriaProcessing() Exception:");
      }
    }

    public void PerformAPIGetAlarmInstructionProcessing(QueueItem qi)
    {
      try
      {
        string summary = "";

        foreach (AlarmInstruction ai in qi.SMS_API.DB.GetAlarmInstructions(qi.AlarmCriteriaID))
        {
          summary = summary + "Instruction " + ai.alarmInstructionID + " for Alarm Criteria #" + ai.alarmCriteriaID + " =  " + ai.description;
        }

        if (summary == "")
          summary = "No Instructions Found for Alarm Criteria #" + qi.AlarmCriteriaID;

        AddLogLine(summary);
      }
      catch (System.Exception ex)
      {
        HandleException(ex, "PerformAPIGetAlarmInstructionProcessing() Exception:");
      }
    }

    public void PerformAPIInsertAlarmCommentProcessing(QueueItem qi)
    {
      try
      {
        bool Success = qi.SMS_API.DB.InsertAlarmComment(qi.TrnHisID, qi.AlarmComment, qi.OperatorName, qi.AlarmCommentDateTime);

        string LogText = "Alarm Comment Insert = '" + qi.AlarmComment + "' for Alarm ID #" + qi.TrnHisID + " by ";

        if (qi.OperatorName.Trim().Length == 0)
          LogText += "_EXP_API";
        else
          LogText += qi.OperatorName.Trim();

        if (Success)
          LogText += " Succeeded";
        else
          LogText += " FAILED";

        AddLogLine(LogText);
      }
      catch (System.Exception ex)
      {
        HandleException(ex, "PerformAPIInsertAlarmCommentProcessing() Exception:");
      }
    }

    public void PerformAPIPortraitProcessing(QueueItem qi)
    {
      if (UseBackgroundThread.InvokeRequired)
      {
        PerformAPIProcessingCallback callback = new PerformAPIProcessingCallback(PerformAPIPortraitProcessing);
        Invoke(callback, qi);
      }
      else
        try
        {
          Cardholder c = new Cardholder(qi.SMS_API);
          c.cardholderID = (int)qi.CardholderID;

          if (c.hasPortrait)
          {
            PortraitLabel.Text = c.portraitPath;
            CardholderPortraitPictureBox.Load(c.portraitPath);
            CardholderPortraitPictureBox.Visible = true;
          }
          else
          {
            PortraitLabel.Text = c.portraitPath + " - Does not exist";
            CardholderPortraitPictureBox.Visible = false;
          }
        }
        catch (System.Exception ex)
        {
          HandleException(ex, "PerformAPIPortraitProcessing() Exception:");
        }
    }

    public void PerformAPIReadAreaAccessProcessing(QueueItem qi)
    {
      try
      {
        int i = 0;

        foreach (AreaAccess a in qi.SMS_API.DB.GetAreaAccesses(null, null, null))
        {
          AddLogLine("Read AreaAccess # " + a.areaAccessID + "' for Area '" + a.areaCaption + "' and Cardholder '" + a.firstname + " " + a.lastname + "'");
          i++;

          if (i >= 50)
            break; // Restrict demo to 50 records
        }

        AddLogLine("Read " + i + " AreaAccesses (max 50 in demo application)");
      }
      catch (System.Exception ex)
      {
        HandleException(ex, "PerformAPIReadAreaAccessProcessing() Exception:");
      }
    }

    public void PerformAPIReadAreaProcessing(QueueItem qi)
    {
      try
      {
        int i = 0;

        foreach (Area a in qi.SMS_API.DB.GetAreas(null, null, null))
        {
          AddLogLine("Read Area # " + a.areaID + "' with caption '" + a.caption + "'");
          i++;

          if (i >= 50)
            break; // Restrict demo to 50 records
        }

        AddLogLine("Read " + i + " Areas (max 50 in demo application)");
      }
      catch (System.Exception ex)
      {
        HandleException(ex, "PerformAPIReadAreaProcessing() Exception:");
      }
    }

    public void PerformAPIReadAreaSetProcessing(QueueItem qi)
    {
      try
      {
        int i = 0;

        foreach (AreaSet a in qi.SMS_API.DB.GetAreaSets(null, null))
        {
          AddLogLine("Read AreaSet # " + a.areaSetID + "' with caption '" + a.caption + "'");
          i++;

          if (i >= 50)
            break; // Restrict demo to 50 records
        }

        AddLogLine("Read " + i + " AreaSets (max 50 in demo application)");
      }
      catch (System.Exception ex)
      {
        HandleException(ex, "PerformAPIReadAreaSetProcessing() Exception:");
      }
    }

    public void PerformAPIReadCameraProcessing(QueueItem qi)
    {
      try
      {
        int i = 0;

        foreach (VideoCamera vc in qi.SMS_API.DB.GetVideoCameras(qi.VideoServerID, qi.DeviceID, qi.TransactionCodeHi, qi.TransactionCodeLo, qi.TransactionCodeID))
        {
          string logLine =
            "Video Camera Control #" + vc.controlID
            + "-------------------------------------"
            + "\n     Caption = '" + vc.controlCaption;

          if (vc.controlDescription != null && vc.controlDescription.Length > 0)
            logLine += "'; with Description = '" + vc.controlDescription + "'";

          logLine += "\n     Camera #" + vc.cameraNumber
            + "\n     TransactionCodeHi = " + vc.transactionCodeHi + " (" + vc.transactionGroup + ")"
            + "\n     TransactionCodeLo (Sum of Transactions) = " + vc.selectedTransactionsSum + "; Selected Transactions = " + vc.selectedTransactions
            + "\n     Time Zone ID = " + vc.timezoneID + "; (" + vc.timezone + ")"
            + "\n     Holiday Set ID = " + vc.holidaySetID + "; (" + vc.holidaySet + ")"
            + "\n     Device ID = " + vc.deviceID
            + "\n     Network Address = " + vc.networkAddress
            + "\n     Video Server Model ID = " + vc.videoServerModelID
            + "\n     Time to Record Before Event = " + vc.videoPreEvent + " sec"
            + "\n     Time to Record After Event = " + vc.videoPostEvent + " sec"
            + "\n     Saved Camera Position = " + vc.cameraPosition + "\n";

          AddLogLine(logLine);
          i++;

          if (i >= 50)
            break; // Restrict demo to 50 records
        }

        AddLogLine("Read " + i + " Camera Controls (max 50 in demo application)");
      }
      catch (System.Exception ex)
      {
        HandleException(ex, "PerformAPIReadCameraProcessing() Exception:");
      }
    }

    public void PerformAPIReadCardholderProcessing(QueueItem qi)
    {
      try
      {
        int i = 0;

        foreach (Cardholder c in qi.SMS_API.DB.GetCardholders(null, qi.FirstName, qi.LastName, null, null, null))
        {
          AddLogLine("**** Valid UDFs: " + GetUDFsAsString(c.GetUserDefinedFields()));
          AddLogLine("**** Credentials: " + GetCredentialsAsString(c.GetCredentials()));
          AddLogLine("Read Cardholder # " + c.cardholderID + "' with name '" + c.lastname + ", " + c.firstname + "'");
          AddLogLine("");
          i++;

          if (i >= 50)
            break; // Restrict demo to 50 records
        }

        AddLogLine("Read " + i + " Cardholders (max 50 in demo application)");
      }
      catch (System.Exception ex)
      {
        HandleException(ex, "PerformAPIReadCardholderProcessing() Exception:");
      }
    }

    public void PerformAPIReadDeletedDeviceProcessing(QueueItem qi)
    {
      try
      {
        int i = 0;

        foreach (Device d in qi.SMS_API.DB.GetDeletedDevices(qi.DeviceID, qi.CaptionPrefix, qi.AreaID, qi.DeviceTypeID, qi.StartDateTime, qi.EndDateTime))
        {
          string logLine = "Device #" + d.deviceID + " with Caption = '" + d.caption + "'; ParentID = " + d.parentID + "; DeviceTypeID = " + d.deviceTypeID
            + " (" + d.deviceTypeCaption + ")" + "; AreaID = " + d.areaID + "; ContactTypeID = " + d.contactTypeID;

          if (d.deviceTypeID == 3)
            logLine += "; Contact Type Caption = " + d.contactTypeCaption;

          logLine += " was Deleted on " + d.modifiedDateTime;

          AddLogLine(logLine);
          i++;

          if (i >= 50)
            break; // Restrict demo to 50 records
        }

        AddLogLine("Read " + i + " Deleted Devices (max 50 in demo application)");
      }
      catch (System.Exception ex)
      {
        HandleException(ex, "PerformAPIReadDeletedDeviceProcessing() Exception:");
      }
    }

    public void PerformAPIReadDeviceProcessing(QueueItem qi)
    {
      try
      {
        int i = 0;

        foreach (Device d in qi.SMS_API.DB.GetDevices(null, null, null, null, null))
        {
          string logLine = "Read Device # " + d.deviceID + "' with caption '" + d.caption + "' and parentID of " + d.parentID + " and MDT: " + d.modifiedDateTime;

          if (d.deviceTypeID == 3)
            logLine += " and of contact type '" + d.contactTypeCaption + '"';

          AddLogLine(logLine);
          i++;

          if (i >= 50)
            break; // Restrict demo to 50 records
        }

        AddLogLine("Read " + i + " Devices (max 50 in demo application)");
      }
      catch (System.Exception ex)
      {
        HandleException(ex, "PerformAPIReadDeviceProcessing() Exception:");
      }
    }

    public void PerformAPIReadDeviceTypeProcessing(QueueItem qi)
    {
      try
      {
        int i = 0;

        foreach (DeviceType dt in qi.SMS_API.DB.GetDeviceTypes())
        {
          AddLogLine("Read DeviceType # " + dt.deviceTypeID + "' with caption '" + dt.caption + "'");
          i++;

          if (i >= 50)
            break; // Restrict demo to 50 records
        }

        AddLogLine("Read " + i + " DeviceTypes (max 50 in demo application)");
      }
      catch (System.Exception ex)
      {
        HandleException(ex, "PerformAPIReadDeviceTypeProcessing() Exception:");
      }
    }

    public void PerformAPIReadMROProcessing(QueueItem qi)
    {
      try
      {
        int i = 0;

        foreach (ManualOverride mro in qi.SMS_API.DB.GetManualOverrides(null, null))
        {
          AddLogLine("Read MRO # " + mro.manualOverrideID + " for device '" + mro.deviceCaption + "' with caption '" + mro.caption + "' and MDT: " + mro.modifiedDateTime);
          i++;

          if (i >= 50)
            break; // Restrict demo to 50 records
        }

        AddLogLine("Read " + i + " MROs (max 50 in demo application)");
      }
      catch (System.Exception ex)
      {
        HandleException(ex, "PerformAPIReadMROProcessing() Exception:");
      }
    }

    public void PerformAPIReadMROSetProcessing(QueueItem qi)
    {
      try
      {
        int i = 0;

        foreach (ManualOverrideSet mroset in qi.SMS_API.DB.GetManualOverrideSets())
        {
          AddLogLine("Read MRO Set# '" + mroset.manualOverrideSetID + "' with caption '" + mroset.caption + "'");
          i++;

          if (i >= 50)
            break; // Restrict demo to 50 records
        }

        AddLogLine("Read " + i + " MRO Sets (max 50 in demo application)");
      }
      catch (System.Exception ex)
      {
        HandleException(ex, "PerformAPIReadMROSetProcessing() Exception:");
      }
    }

    public void PerformAPIReadTransAlarmProcessing(QueueItem qi)
    {
      try
      {
        int i = 0;

        foreach (Alarm a in qi.SMS_API.DB.GetAlarms(qi.StartDateTime, qi.EndDateTime, qi.CardholderID, qi.AreaID, qi.DeviceID))
        {
          String ackDesc;

          if (a.isAcknowledged)
            ackDesc = "acknowledged";
          else
            ackDesc = "unacknowledged";

          AddLogLine("Read Alarm # " + a.trnHisID + ". Occurred at '" + a.transactionDateTime + "' of type '" + a.transactionCode.caption + "' for Area '" + a.area.caption + "'. Alarm is " + ackDesc);
          i++;

          if (i >= 50)
            break; // Restrict demo to 50 records
        }

        AddLogLine("Read " + i + " records (max 50 in demo application)");
      }
      catch (System.Exception ex)
      {
        HandleException(ex, "PerformAPIReadTransAlarmProcessing() Exception:");
      }
    }

    public void PerformAPIReadTransCodeProcessing(QueueItem qi)
    {
      try
      {
        int i = 0;

        foreach (TransactionCode tc in qi.SMS_API.DB.GetTransactionCodes())
        {
          AddLogLine("Read TransactionCode # '" + tc.transactionCodeID + "' in group '" + tc.transactionCodeHi + "' with caption '" + tc.caption + "'");
          i++;

          if (i >= 50)
            break; // Restrict demo to 50 records
        }

        AddLogLine("Read " + i + " transaction codes (max 50 in demo application)");
      }
      catch (System.Exception ex)
      {
        HandleException(ex, "PerformAPIReadTransCodeProcessing() Exception:");
      }
    }

    public void PerformAPIReadTransGroupProcessing(QueueItem qi)
    {
      try
      {
        int i = 0;

        foreach (TransactionGroup tg in qi.SMS_API.DB.GetTransactionGroups())
        {
          AddLogLine("Read Transaction Group # '" + tg.transactionCodeHi + "' with caption '" + tg.caption + "'");
          i++;

          if (i >= 50)
            break; // Restrict demo to 50 records
        }

        AddLogLine("Read " + i + " transaction groups (max 50 in demo application)");
      }
      catch (System.Exception ex)
      {
        HandleException(ex, "PerformAPIReadTransGroupProcessing() Exception:");
      }
    }

    public void PerformAPIReadTransProcessing(QueueItem qi)
    {
      try
      {
        int i = 0;

        foreach (Transaction t in qi.SMS_API.DB.GetTransactions(qi.StartDateTime, qi.EndDateTime, qi.CardholderID, qi.AreaID, qi.DeviceID))
        {
          AddLogLine("Read Transaction # " + t.trnHisID + ". Occurred at '" + t.transactionDateTime + "' of type '" + t.transactionCode.caption + "' for Area '" + t.area.caption + "'");
          i++;

          if (i >= 50)
            break; // Restrict demo to 50 records
        }

        AddLogLine("Read " + i + " records (max 50 in demo application)");
      }
      catch (System.Exception ex)
      {
        HandleException(ex, "PerformAPIReadTransProcessing() Exception:");
      }
    }

    public void PerformAPIReadVideoServerProcessing(QueueItem qi)
    {
      try
      {
        int i = 0;

        foreach (VideoServer vs in qi.SMS_API.DB.GetVideoServers())
        {
          string logLine = "Video Server #" + vs.ModelID + " with Caption = '" + vs.VideoServerModel + "';";

          if (vs.Description != null && vs.Description.Length > 0)
            logLine += " Description = '" + vs.Description + "';";

          logLine += " Network Address = " + vs.NetworkAddress;

          AddLogLine(logLine);
          i++;

          if (i >= 50)
            break; // Restrict demo to 50 records
        }

        AddLogLine("Read " + i + " Video Servers (max 50 in demo application)");
      }
      catch (System.Exception ex)
      {
        HandleException(ex, "PerformAPIReadVideoServerProcessing() Exception:");
      }
    }

    public void PerformAPIResetAntiPassbackProcessing(QueueItem qi)
    {
      try
      {
        qi.SMS_API.ResetAntipassbackStatus(qi.EncodedID);
      }
      catch (System.Exception ex)
      {
        HandleException(ex, "PerformAPIResetAntiPassbackProcessing() Exception:");
      }
    }

    /// <summary> Refresh Form Data </summary>
    /// <remarks> Enable/disable buttons, status color and other form elements where display is
    /// dependent on internal state. </remarks>
    void RefreshFormData()
    {
      if (UseBackgroundThread.InvokeRequired)
      {
        RefreshFormDataCallback callback = new RefreshFormDataCallback(RefreshFormData);
        Invoke(callback);
      }
      else
      {
        if (_connectedToSystemProcessor)
        {
          SPStatusTextBox.Text = "Connected";
          SPStatusTextBox.BackColor = SystemColors.Control;
          SPStatusTextBox.ForeColor = System.Drawing.Color.Green;
        }
        else
        {
          SPStatusTextBox.Text = "Disconnected";
          SPStatusTextBox.BackColor = SystemColors.Control;
          SPStatusTextBox.ForeColor = System.Drawing.Color.Red;
        }

        if (_connectedToDatabase)
        {
          DBStatusTextBox.Text = MROglobal;
          DBStatusTextBox.BackColor = SystemColors.Control;
          DBStatusTextBox.ForeColor = System.Drawing.Color.Green;
          QueueItem qi = new QueueItem();

          qi.TransactionKind = QueueItem.Kind.ExecuteMRO;
          qi.OperatorName = OperatorNameTextBox.Text;
          //qi.OverrideID = Convert.ToInt32(MRONumericUpDown.Value);
          int x = Int32.Parse(MROglobal);
          qi.OverrideID = Convert.ToInt32(x);
          //Application.Exit();


          if (UseBackgroundThread.Checked)
          {
              TransactionQueue.EnqueueItem(qi);
             // Application.Exit();
          }
          else
          {
              // Use the SMS_API instance created when the OpenConnectionButton button was clicked.
              qi.SMS_API = _SMS_API;

              PerformAPIExecuteMROProcessing(qi);
          }
           
          ExecuteMROButton.PerformClick();
        }
        else
        {
          DBStatusTextBox.Text = "Disconnected";
          DBStatusTextBox.BackColor = SystemColors.Control;
          DBStatusTextBox.ForeColor = System.Drawing.Color.Red;
        }

        bool SMS_APIPresent = false;

        if ((UseBackgroundThread.Checked && HasTransactionQueueInstance() && TransactionQueue.Enabled) || (!UseBackgroundThread.Checked && HasSMS_APIInstance()))
          SMS_APIPresent = true;

        OpenConnectionButton.Enabled = !SMS_APIPresent;
        CloseConnectionButton.Enabled = SMS_APIPresent;
        ValidateCardholderPortraitButton.Enabled = SMS_APIPresent;

        ExecuteMROButton.Enabled = (SMS_APIPresent && MRONumericUpDown.Value != 0 && _connectedToDatabase && _connectedToSystemProcessor);
        ExecuteMROSetButton.Enabled = (SMS_APIPresent && MROSetNumericUpDown.Value != 0 && _connectedToDatabase && _connectedToSystemProcessor);
        AcknowlegeAlarmButton.Enabled = (SMS_APIPresent && AlarmNumericUpDown.Value != 0 && _connectedToDatabase && _connectedToSystemProcessor);
        RequestDeviceStatusButton.Enabled = (SMS_APIPresent && StatusNumericUpDown.Value != 0 && _connectedToDatabase && _connectedToSystemProcessor);
        ResetAPButton.Enabled = (SMS_APIPresent && APEncodedIDNumericUpDown.Value != 0 && _connectedToDatabase && _connectedToSystemProcessor);
        //OpenConnectionButton.PerformClick();
      }
      //OpenConnectionButton.PerformClick();
    }


    public SMS_API_DemoForm(string[] args)
    {
        
        MROglobal = args[0];
       
      InitializeComponent();
      Load += Form1_Shown;
      
      DBStatusTextBox.ReadOnly = true;
      SPStatusTextBox.ReadOnly = true;
      //OpenConnectionButton.PerformClick();
     
    }
    private void Form1_Shown(Object sender, EventArgs e)
    {
        OpenConnectionButton.PerformClick();
        //ExecuteMROButton.PerformClick();

        if (System.Windows.Forms.Application.MessageLoop)
        {
            // WinForms app
           // System.Windows.Forms.Application.Exit();
        }
        else
        {
            // Console app
           // System.Environment.Exit(1);
        }
    }

    /// <summary>Form Closing Event Handler </summary>
    /// <param name="sender">object</param>
    /// <param name="e">FormClosingEventArgs</param>
    /// <Created>2012-02-08</Created>
    void SMS_API_Demo_Main_FormClosing(object sender, FormClosingEventArgs e)
    {
      // Save current connection settings on exit.
      WriteINIValues();

      // Must include CloseAPI() here for APITransactionQueue.
      CloseAPI();
    }

    /// <summary> Form Load Event Handler </summary>
    /// <param name="sender">object</param>
    /// <param name="e">EventArgs</param>
    void SMS_API_Demo_Main_Load(object sender, EventArgs e)
    {
      // Load Last Connection Settings
      if (!LoadINIValues())
      {
        string message = "Error Reading " + _iniFile;
        string caption = "Error Loading Last Connection Settings";
        
        MessageBox.Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
        //OpenConnectionButton.PerformClick();
      }
     // OpenConnectionButton.PerformClick();
      RefreshFormData();
      //OpenConnectionButton.PerformClick();
    }

    /// <summary>Used to enable or disable connection-related GUI fields</summary>
    /// <param name="enabled"></param>
    void SetConnectionFieldEnabledStates(bool enabled)
    {
      OpenConnectionButton.Enabled = enabled;
      UseBackgroundThread.Enabled = enabled;

      SetControlEnabledStates(ConnectionEditFields, enabled);
      OpenConnectionButton.PerformClick();
    }

    /// <summary>Used to set the cursor of the form and other controls</summary>
    /// <param name="cursor"></param>
    void SetControlCursor(Cursor cursor)
    {
      Cursor = cursor;

      // We need to set these controls independently because they are not initially disabled (design-time),
      // and are not disabled when the user clicks the OpenConnectionButton button.
      ClearLogButton.Cursor = cursor;
      DBStatusTextBox.Cursor = cursor;
      LogRichTextBox.Cursor = cursor;
      LookupCheckBox.Cursor = cursor;
      SPStatusTextBox.Cursor = cursor;
    }

    /// <summary>Used to enable or disable all controls within a container (parentControl), without
    /// affecting the enabled state of the container itself</summary>
    /// <param name="parentControl"</param>The container who's child controls will be recursively processed
    /// <param name="enabled"</param>Enabled state
    void SetControlEnabledStates(Control parentControl, bool enabled)
    {
      if (parentControl == null)
        return;

      // Get a collection of child controls for parentControl.
      var controls = FormHelper.EnumControls<Control>(parentControl);

      // Update the Enabled state of each control.
      foreach (var control in controls)
      {
        if (control != parentControl)
          control.Enabled = enabled;
      }
    }

    /// <summary>Used to enable or disable all non-connection-related GUI fields</summary>
    /// <param name="enabled"></param>
    void SetNonConnectionFieldEnabledStates(bool enabled)
    {
      if (UseBackgroundThread.InvokeRequired)
      {
        SetEnabledStatesCallback callback = new SetEnabledStatesCallback(SetNonConnectionFieldEnabledStates);
        Invoke(callback, enabled);
      }
      else
      {
        SetControlEnabledStates(RealTimeGroupBox, enabled);
        SetControlEnabledStates(DatabaseTabPage, enabled);
        SetControlEnabledStates(VideoTabPage, enabled);
      }
    }

    /// <summary> Convert Integer to Valid Range </summary>
    /// <param name="value">Integer Value</param>
    /// <returns>Null or Value Value</returns>
    int? UserIntToInt(int value)
    {
      if (value < -2)
        return null;
      else
        return value;
    }

    /// <summary> Convert String to Datetime </summary>
    /// <param name="s">Date String</param>
    /// <returns>Datetime</returns>
    DateTime? UserStringToDateTime(String s)
    {
      if ((s == "") || (s == null))
        return null;
      else
        return Convert.ToDateTime(s);
    }

    /// <summary> Write Connection Data to SMS_API INI file </summary>
    /// <remarks> Create File If It Doesn't Exist </remarks>
    /// <returns>True if Successful</returns>
    /// <Created>2012-02-08</Created>
    bool WriteINIValues()
    {
      String[] iniData = new string[]
      {
        "SPHostName = " + this.SPHostnameTextBox.Text,
        "DBHostName = " + this.DBHostnameTextBox.Text,
        "DBName = " + this.DBNameTextBox.Text,
        "DBLogin = " + this.DBLoginTextBox.Text,
        "Password = " + this.PasswordTextBox.Text,
        "DataDirectory = " + this.DataDirectoryTextBox.Text
      };

      try
      {
        File.WriteAllLines(_iniFile, iniData);
        return true;
      }
      catch
      {
        return false;
      }
    }

    /// <summary>Used to pass connection-related values when a background thread is being used to communicate with
    /// the database & System Processor</summary>
    public struct ConnectionInfo
    {
      public string DataDirectory;
      public string DBHostname;
      public string DBLogin;
      public string DBName;
      public string DBPassword;
      public int DBReconnectInterval;
      public string EventLogSource;
      public string SPHostname;
      public bool UseVerboseDebugLogging;
    }

    /// <summary> APITransactionQueue class </summary>
    /// <remarks> Used when the UseBackgroundThread checkbox is checked, to queue-up requests made from the GUI.
    /// The requests are processed one by one using a separate thead.  Updates to the GUI are synchronized.</remarks>
    class APITransactionQueue : IDisposable
    {
      #region Fields
        Thread _backgroundThread;
        ConnectionInfo _connectionInfo;
        bool _enabled;
        bool _allowNullQueueItem;
        Queue<QueueItem> _itemQueue;
        readonly object _lockObject;
        SMS.SMS_API _SMS_API;
        ITransactionQueueAPISupport _SMS_APISupport;
        EventWaitHandle _waitHandle;
        bool _waiting;
      #endregion

      #region Properties
        /// <summary>Enabled property</summary>
        /// <remarks>Indicates or sets whether the queue is active</remarks>
        public bool Enabled
        {
          get
          {
            lock(_lockObject)
            {
              return _enabled;
            }
          }

          private set
          {
            lock(_lockObject)
            {
              _enabled = value;
            }
          }
        }

        /// <summary>SMS_API property</summary>
        /// <remarks>Returns the SMS_API instance create by the queue</remarks>
        public SMS.SMS_API SMS_API
        {
          get
          {
            lock(_lockObject)
            {
              return _SMS_API;
            }
          }

          private set
          {
            lock(_lockObject)
            {
              _SMS_API = value;
            }
          }
        }

        /// <summary>Waiting property</summary>
        /// <remarks>Indicates or sets whether the queue is currently waiting for a queue item</remarks>
        bool Waiting
        {
          get
          {
            lock(_lockObject)
            {
              return _waiting;
            }
          }

          set
          {
            lock(_lockObject)
            {
              _waiting = value;
            }
          }
        }
      #endregion

      /// <summary>APITransactionQueue constructor</summary>
      /// <param name="SMS_APISupport"></param>
      /// <param name="connectionInfo"</param>
      public APITransactionQueue(ITransactionQueueAPISupport SMS_APISupport, ConnectionInfo connectionInfo)
      {
        #region Validate SMS_APISupport parameter
        // The SMS_APISupport parameter provides a way for us to call the methods that invoke the SMS.SMS_API
        // methods (see the various PerformAPIXXXProcessing() methods of the SMS_API_Demo_Main class).
        #endregion
        if (SMS_APISupport == null)
          throw new Exception("APITransactionQueue: Parameter \"SMS_APISupport\" invalid.");

        // Store connection-related values provided by caller.  These values are used to connect to the database
        // and the System Processor etc.
        _connectionInfo = connectionInfo;

        // Store reference to interface containing methods that we'll use to perform API calls.
        _SMS_APISupport = SMS_APISupport;

        _itemQueue = new Queue<QueueItem>();
        _lockObject = new object();
        _waitHandle = new AutoResetEvent(false);

        // Background thread used to process QueueItem(s) placed on the queue.
        // Note: It's not recommended to set "_backgroundThread.IsBackground = true".
        _backgroundThread = new Thread(ProcessItems);
        _backgroundThread.SetApartmentState(ApartmentState.STA);
        _backgroundThread.Start();
      }

      /// <summary>Close API Connections</summary>
      void CloseAPI()
      {
        try
        {
          lock(_lockObject)
          {
            if (_SMS_API != null)
            {
              // Dispose forces unmanaged resources within the Win32 SPComm.dll to be freed immediately.
              // Otherwise, the resources may not be available if a new SMS_API object is created.
              _SMS_API.Dispose();
              _SMS_API = null;
            }
          }
        }
        catch (System.Exception ex)
        {
          _SMS_APISupport.HandleTransactionQueueException("Error attempting to close SMS API with exception:\n\n" + ex.Message + "\n", ex);
        }
      }

      /// <summary>Dispose method</summary>
      /// <remarks>Implement IDisposable.Dispose()</remarks>
      public void Dispose()
      {
        EndProcessing();

        // Release O/S resources.
        _waitHandle.Close();
      }

      /// <summary> EndProcessing method</summary>
      /// <remarks>Used to cause processing of queue items to cease.
      /// When EndProcessing() is called, no additional QueueItem(s) are processed from the queue, the background
      /// thread used to process the queue items is joined to wait for its completion, and control is then
      /// returned to the caller.</remarks>
      public void EndProcessing()
      {
        lock(_lockObject)
        {
          if (Enabled)
          {
            // Change Enabled state.  This prevents any QueueItem-processing code blocks within the main loop of ProcessItems()
            // from being executed (and we fall-through to the bottom of the loop).
            Enabled = false;

            // If we're waiting for a signal (a QueueItem to be placed on the queue), enqueue a null QueueItem.
            if (Waiting)
            {
              // Set field to internally allow a null to be accepted by the EnqueueItem() method.
              _allowNullQueueItem = true;

              try
              {
                EnqueueItem(null);
              }
              finally
              {
                _allowNullQueueItem = false;
              }
            }
          }
          else
            return;
        }
      }

      /// <summary>EnqueueItem method</summary>
      /// <param name="qi"></param>
      /// <remarks>Used to place a new QueueItem instance on the queue</remarks>
      public void EnqueueItem(QueueItem qi)
      {
        // Do not allow a null QueueItem instance unless we're passing one from EndProcessing().
        if (qi == null && !_allowNullQueueItem)
          return;

        lock(_lockObject)
        {
          _itemQueue.Enqueue(qi);
          _waitHandle.Set();
        }
      }

      /// <summary>ProcessItems method</summary>
      /// <remarks>Processes queue items in a FIFO fashion</remarks>
      unsafe void ProcessItems()
      {
        try
        {
          // Perform notification that queue is starting-up.
          _SMS_APISupport.HandleTransactionQueueStartup();

          // Declare and initialize returnCode that's returned by reference (via pointer) from SMS.SMS_API() constructor below.
          int rc = 0;
          int* returnCode = &rc;

          try
          {
            // Perform notification that connection is about to take place.
            _SMS_APISupport.HandleAPIBeforeConnection();

            // Create SMS_API instance using connection values passed to APITransactionQueue constructor.
            _SMS_API = new SMS.SMS_API(
              _connectionInfo.SPHostname,
              _connectionInfo.DBHostname,
              _connectionInfo.DBName,
              _connectionInfo.DBLogin,
              _connectionInfo.DBPassword,
              _connectionInfo.DBReconnectInterval,
              _connectionInfo.UseVerboseDebugLogging,
              _connectionInfo.EventLogSource,
              _connectionInfo.DataDirectory,
              returnCode
            );
          }
          catch (System.Exception ex)
          {
            _SMS_APISupport.HandleTransactionQueueException("Call to create new instance of SMS API raised exception:\n\n" + ex.Message + "\n", ex);

            // Set returnCode to a non-zero value (for failure) for "finally" clause below.
            *returnCode = -999;
          }
          finally
          {
            if (*returnCode == 0)
            {
              // Change Enabled state.  This shows that we have active connections to the SMS_API & database, and allows processing to take place.
              Enabled = true;

              // Perform notification that we're successfully connected to the SMS_API & database.
              _SMS_APISupport.HandleAPIAfterConnection(true);

              // Attach Event Handlers implemented by ITransactionQueueAPISupport interface.
              _SMS_API.connectionStatusChangedHandler += _SMS_APISupport.HandleAPIConnectionStatusChange;
              _SMS_API.transactionHandler += _SMS_APISupport.HandleAPITransaction;
              _SMS_API.alarmHandler += _SMS_APISupport.HandleAPIAlarm;
              _SMS_API.mROExecutionCompleteHandler += _SMS_APISupport.HandleAPIMROExecutionComplete;
              _SMS_API.alarmKillHandler += _SMS_APISupport.HandleAPIAlarmKill;
              _SMS_API.alarmAcknowledgementHandler += _SMS_APISupport.HandleAPIAlarmAcknowledgement;
              _SMS_API.databaseChangeHandler += _SMS_APISupport.HandleAPIDatabaseChange;
              _SMS_API.deviceStatusChangeHandler += _SMS_APISupport.HandleAPIDeviceStatusChange;
              _SMS_API.DB.commandTimeoutInSeconds = 600;
            }
            else
            {
              // Perform notification that connections to the SMS_API & database were unsuccessful.
              _SMS_APISupport.HandleAPIAfterConnection(false);

              if (*returnCode != -999)
                _SMS_APISupport.HandleTransactionQueueException("Call to create new instance of SMS API failed with message:\n\n" + _SMS_API.ReturnCodeToString(*returnCode) + "\n", null);
            }
          }

          #region Processing Loop
            while (Enabled)
            {
              QueueItem qi = null;

              // Get an item from the queue.
              lock(_lockObject)
              {
                if (Enabled && _itemQueue.Count > 0)
                  qi = _itemQueue.Dequeue();
              }

              if (Enabled && qi != null)
              {
                // Process QueueItem(s).

                // Use the SMS_API instance created by the queue.
                qi.SMS_API = _SMS_API;

                // Based-on the item's TransactionKind, call the appropriate method of the ITransactionQueueAPISupport interface.
                switch (qi.TransactionKind)
                {
                  case QueueItem.Kind.AcknowledgeAlarm:
                    _SMS_APISupport.PerformAPIAcknowledgeAlarmProcessing(qi);
                    break;

                  case QueueItem.Kind.ExecuteMRO:
                    _SMS_APISupport.PerformAPIExecuteMROProcessing(qi);
                    break;

                  case QueueItem.Kind.ExecuteMROSet:
                    _SMS_APISupport.PerformAPIExecuteMROSetProcessing(qi);
                    break;

                  case QueueItem.Kind.GetAlarmComments:
                    _SMS_APISupport.PerformAPIGetAlarmCommentProcessing(qi);
                    break;

                  case QueueItem.Kind.GetAlarmCriteria:
                    _SMS_APISupport.PerformAPIGetAlarmCriteriaProcessing(qi);
                    break;

                  case QueueItem.Kind.GetAlarmInsructions:
                    _SMS_APISupport.PerformAPIGetAlarmInstructionProcessing(qi);
                    break;

                  case QueueItem.Kind.InsertAlarmComment:
                    _SMS_APISupport.PerformAPIInsertAlarmCommentProcessing(qi);
                    break;

                  case QueueItem.Kind.ProcessPortrait:
                    _SMS_APISupport.PerformAPIPortraitProcessing(qi);
                    break;

                  case QueueItem.Kind.ReadAreaAccesses:
                    _SMS_APISupport.PerformAPIReadAreaAccessProcessing(qi);
                    break;

                  case QueueItem.Kind.ReadAreas:
                    _SMS_APISupport.PerformAPIReadAreaProcessing(qi);
                    break;

                  case QueueItem.Kind.ReadAreaSets:
                    _SMS_APISupport.PerformAPIReadAreaSetProcessing(qi);
                    break;

                  case QueueItem.Kind.ReadCameras:
                    _SMS_APISupport.PerformAPIReadCameraProcessing(qi);
                    break;

                  case QueueItem.Kind.ReadCardholders:
                    _SMS_APISupport.PerformAPIReadCardholderProcessing(qi);
                    break;

                  case QueueItem.Kind.ReadDeletedDevices:
                    _SMS_APISupport.PerformAPIReadDeletedDeviceProcessing(qi);
                    break;

                  case QueueItem.Kind.ReadDevices:
                    _SMS_APISupport.PerformAPIReadDeviceProcessing(qi);
                    break;

                  case QueueItem.Kind.ReadDeviceTypes:
                    _SMS_APISupport.PerformAPIReadDeviceTypeProcessing(qi);
                    break;

                  case QueueItem.Kind.ReadMRO:
                    _SMS_APISupport.PerformAPIReadMROProcessing(qi);
                    break;

                  case QueueItem.Kind.ReadMROSets:
                    _SMS_APISupport.PerformAPIReadMROSetProcessing(qi);
                    break;

                  case QueueItem.Kind.ReadTrans:
                    _SMS_APISupport.PerformAPIReadTransProcessing(qi);
                    break;

                  case QueueItem.Kind.ReadTransAlarm:
                    _SMS_APISupport.PerformAPIReadTransAlarmProcessing(qi);
                    break;

                  case QueueItem.Kind.ReadTransCodes:
                    _SMS_APISupport.PerformAPIReadTransCodeProcessing(qi);
                    break;

                  case QueueItem.Kind.ReadTransGroups:
                    _SMS_APISupport.PerformAPIReadTransGroupProcessing(qi);
                    break;

                  case QueueItem.Kind.ReadVideoServers:
                    _SMS_APISupport.PerformAPIReadVideoServerProcessing(qi);
                    break;

                  case QueueItem.Kind.RequestDeviceStatus:
                    _SMS_APISupport.PerformAPIDeviceStatusProcessing(qi);
                    break;

                  case QueueItem.Kind.ResetAntiPassback:
                    _SMS_APISupport.PerformAPIResetAntiPassbackProcessing(qi);
                    break;
                }
              }
              else if (Enabled)
              {
                // Wait for first/next queue item.
                Waiting = true;

                try
                {
                  // Note: We must check Enabled because we could have been waiting for a lock to release
                  // on the "Waiting = true" line above (and now Enabled may not be true).  This situation can occur
                  // when EndProcessing() is called by another thread (in this case, the demo form).
                  if (Enabled)
                  {
                    _waitHandle.WaitOne(125);

                    // Message pump required for real-time messaging from System Processor.
                    Application.DoEvents();
                  }
                }
                finally
                {
                  Waiting = false;
                }
              }
            }
          #endregion
        }
        catch (System.Exception ex)
        {
          _SMS_APISupport.HandleTransactionQueueException("The following exception occurred while processing transaction queue items:\n\n" + ex.Message + "\n", ex);
        }
        finally
        {
          CloseAPI();

          // Perform notification that queue is shutting-down.
          _SMS_APISupport.HandleTransactionQueueShutdown();
        }
      }
    }
  }

  /// <summary>Helper class for SMS_API_Demo_Main</summary>
  public static class FormHelper
  {
    /// <summary>Used to recursively iterate through all controls contained within an overall parent control</summary>
    /// <typeparam name="T"></typeparam>The type of control to return (like Checkbox or Button, for example)
    /// <param name="parentControl"></param>The initial (parent) control to examine
    /// <returns>IEnumerable - collection of controls</returns>
    public static IEnumerable<T> EnumControls<T>(this Control parentControl) where T : Control
    {
      if (parentControl == null)
        yield break;

      if (parentControl is T)
        yield return parentControl as T;

      foreach (var child in parentControl.Controls.Cast<Control>())
      {
        foreach (var item in EnumControls<T>(child))
        {
          yield return item;
        }
      }
    }
  }

  /// <summary> QueueItem class </summary>
  /// <remarks> Used to pass information to the APITransactionQueue via its EnqueueItem() method
  /// when the UseBackgroundThread checkbox is checked, and to the various PerformAPIXXXProcessing() methods.</remarks>
  public class QueueItem : Object
  {
    /// <summary>Used to indicate the kind of SMS_API feature requested when the UseBackgroundThread checkbox is checked</summary>
    public enum Kind
    {
      AcknowledgeAlarm, ExecuteMRO, ExecuteMROSet, GetAlarmComments, GetAlarmCriteria,
      GetAlarmInsructions, InsertAlarmComment, ProcessPortrait, ReadAreaAccesses, ReadAreas, ReadAreaSets,
      ReadCameras, ReadCardholders, ReadDeletedDevices, ReadDevices, ReadDeviceTypes, ReadMRO,
      ReadMROSets, ReadTrans, ReadTransAlarm, ReadTransCodes, ReadTransGroups, ReadVideoServers,
      RequestDeviceStatus, ResetAntiPassback
    };

    public string AlarmComment;
    public DateTime AlarmCommentDateTime;
    public int AlarmCriteriaID;
    public SMS.SMS_API SMS_API;
    public int? AreaID;
    public string CaptionPrefix;
    public int? CardholderID;
    public int? DeviceID;
    public int? DeviceTypeID;
    public uint EncodedID;
    public DateTime? EndDateTime;
    public string FirstName;
    public string LastName;
    public string OperatorName;
    public int OverrideID;
    public DateTime? StartDateTime;
    public int? TransactionCodeHi;
    public int? TransactionCodeID;
    public int? TransactionCodeLo;
    public Kind TransactionKind;
    public int TrnHisID;
    public int? VideoServerID;
  }

  /// <summary>Identifies methods that must be implemented in order to create an instance of the APITransactionQueue class.</summary>
  /// <remarks>Each method of the interface represents a specific feature of the SMS_API.
  ///
  /// In this demo application, when the user checks the UseBackgroundThread checkbox and clicks on the various
  /// buttons to exercise the SMS_API, a queue of class APITransactionQueue is created (single instance) as a
  /// way to temporarily buffer those requests.
  ///
  /// One of the parameters of the APITransactionQueue's constructor is the ITransactionQueueCallSupport interface.
  /// The queue processes each entry, examines it's Kind (see QueueItem.Kind) enumeration to determine which method
  /// of the ITransactionQueueCallSupport interface should be called, and then calls that method.
  ///
  /// The PerformAPIXXXProcessing() methods of the SMS_API_Demo_Main class provide the implementation of the
  /// ITransactionQueueCallSupport interface for this demo application.
  /// </remarks>
  interface ITransactionQueueAPISupport
  {
    // Note: All methods are called in the context of the APITransactionQueue's background thread.

    // Event Handlers that must be implemented for the SMS_API.  C# SMS_API.dll will call these handlers.
    void HandleAPIAlarmAcknowledgement(int trnHisID, Operator acknowledger, DateTime acknowledgedDateTime);
    void HandleAPIAlarm(Alarm alarm);
    void HandleAPIAlarmKill(int trnHisID);
    void HandleAPIConnectionStatusChange(bool connectedToSP, bool connectedToDatabase);
    void HandleAPIDatabaseChange(uint changedDatabaseTablesBitmap);
    void HandleAPIDeviceStatusChange(DeviceStatusMessage deviceStatusMessage);
    void HandleAPIMROExecutionComplete(int trnHisID, int statusCode, string statusMessage);
    void HandleAPITransaction(Transaction transaction);

    // Event Handler for processing prior to connection to SMS_API & database.
    void HandleAPIBeforeConnection();

    // Event Handler for processing after connection attempt to SMS_API & database.
    void HandleAPIAfterConnection(bool connected);

    // Event Handler for exceptions.
    void HandleTransactionQueueException(string errorMessage, System.Exception ex);

    // Event Handler for processing just after queue is first instantiated.  Occurs before HandleAPIBeforeConnection().
    void HandleTransactionQueueStartup();

    // Event Handler for processing when queue is shutting-down.
    void HandleTransactionQueueShutdown();

    // Methods that must be implemented in order to perform background processing using the the APITransactionQueue class.
    void PerformAPIAcknowledgeAlarmProcessing(QueueItem qi);
    void PerformAPIPortraitProcessing(QueueItem qi);
    void PerformAPIDeviceStatusProcessing(QueueItem qi);
    void PerformAPIExecuteMROProcessing(QueueItem qi);
    void PerformAPIExecuteMROSetProcessing(QueueItem qi);
    void PerformAPIGetAlarmCommentProcessing(QueueItem qi);
    void PerformAPIGetAlarmCriteriaProcessing(QueueItem qi);
    void PerformAPIGetAlarmInstructionProcessing(QueueItem qi);
    void PerformAPIInsertAlarmCommentProcessing(QueueItem qi);
    void PerformAPIReadAreaAccessProcessing(QueueItem qi);
    void PerformAPIReadAreaProcessing(QueueItem qi);
    void PerformAPIReadAreaSetProcessing(QueueItem qi);
    void PerformAPIReadCameraProcessing(QueueItem qi);
    void PerformAPIReadCardholderProcessing(QueueItem qi);
    void PerformAPIReadDeletedDeviceProcessing(QueueItem qi);
    void PerformAPIReadDeviceProcessing(QueueItem qi);
    void PerformAPIReadDeviceTypeProcessing(QueueItem qi);
    void PerformAPIReadMROProcessing(QueueItem qi);
    void PerformAPIReadMROSetProcessing(QueueItem qi);
    void PerformAPIReadTransAlarmProcessing(QueueItem qi);
    void PerformAPIReadTransCodeProcessing(QueueItem qi);
    void PerformAPIReadTransGroupProcessing(QueueItem qi);
    void PerformAPIReadTransProcessing(QueueItem qi);
    void PerformAPIReadVideoServerProcessing(QueueItem qi);
    void PerformAPIResetAntiPassbackProcessing(QueueItem qi);
  }
}
