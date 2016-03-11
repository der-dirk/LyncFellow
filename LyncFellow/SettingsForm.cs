using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using Microsoft.Lync.Model;

namespace LyncFellow
{
  public partial class SettingsForm : Form
  {
    private ViewModel _model;


    public SettingsForm(ViewModel model)
    {
      InitializeComponent();

      _model = model;

      Icon = Properties.Resources.LyncFellow;
      Text += string.Format(" (Version {0})", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);

      if (Properties.Settings.Default.RedOnTrackerError)
      {
        OnTrackerError.Checked = true;
        CITrackerPath.Enabled = true;
      }
      else
      {
        CITrackerPath.Enabled = false;

        switch (Properties.Settings.Default.RedOnDndCallBusy)
        {
          case ContactAvailability.DoNotDisturb:
            OnDoNotDisturb.Checked = true;
            break;
          case ContactAvailability.Busy:
            OnBusy.Checked = true;
            break;
          default:
            OnCallConference.Checked = true;
            break;
        }
      }

      IncomingCall_DoDance.Checked = Properties.Settings.Default.IncomingCall_DoDance;
      IncomingCall_DoFlapWings.Checked = Properties.Settings.Default.IncomingCall_DoFlapWings;
      IncomingCall_DoGlowHeart.Checked = Properties.Settings.Default.IncomingCall_DoGlowHeart;
      IncomingCall_DoBlinkSingleColor.Checked = Properties.Settings.Default.IncomingCall_DoBlinkSingleColor;
      IncomingCall_DoBlinkRainbow.Checked = Properties.Settings.Default.IncomingCall_DoBlinkRainbow;

      NewIMConversation_DoDance.Checked = Properties.Settings.Default.NewIMConversation_DoDance;
      NewIMConversation_DoFlapWings.Checked = Properties.Settings.Default.NewIMConversation_DoFlapWings;
      NewIMConversation_DoGlowHeart.Checked = Properties.Settings.Default.NewIMConversation_DoGlowHeart;
      NewIMConversation_DoBlinkSingleColor.Checked = Properties.Settings.Default.NewIMConversation_DoBlinkSingleColor;
      NewIMConversation_DoBlinkRainbow.Checked = Properties.Settings.Default.NewIMConversation_DoBlinkRainbow;

      CITrackerPath.Text = Properties.Settings.Default.CITrackerPath;
    }

    private void CloseButtton_Click(object sender, EventArgs e)
    {
      Properties.Settings.Default.IncomingCall_DoDance = IncomingCall_DoDance.Checked;
      Properties.Settings.Default.IncomingCall_DoFlapWings = IncomingCall_DoFlapWings.Checked;
      Properties.Settings.Default.IncomingCall_DoGlowHeart = IncomingCall_DoGlowHeart.Checked;
      Properties.Settings.Default.IncomingCall_DoBlinkSingleColor = IncomingCall_DoBlinkSingleColor.Checked;
      Properties.Settings.Default.IncomingCall_DoBlinkRainbow = IncomingCall_DoBlinkRainbow.Checked;

      Properties.Settings.Default.NewIMConversation_DoDance = NewIMConversation_DoDance.Checked;
      Properties.Settings.Default.NewIMConversation_DoFlapWings = NewIMConversation_DoFlapWings.Checked;
      Properties.Settings.Default.NewIMConversation_DoGlowHeart = NewIMConversation_DoGlowHeart.Checked;
      Properties.Settings.Default.NewIMConversation_DoBlinkSingleColor = NewIMConversation_DoBlinkSingleColor.Checked;
      Properties.Settings.Default.NewIMConversation_DoBlinkRainbow = NewIMConversation_DoBlinkRainbow.Checked;

      Properties.Settings.Default.CITrackerPath = CITrackerPath.Text;
      
      Properties.Settings.Default.RedOnTrackerError = OnTrackerError.Checked;
      if (OnTrackerError.Checked)
      {
        Properties.Settings.Default.RedOnDndCallBusy = ContactAvailability.None;
      }
      else
      {
        if (OnDoNotDisturb.Checked)
        {
          Properties.Settings.Default.RedOnDndCallBusy = ContactAvailability.DoNotDisturb;
        }
        else if (OnBusy.Checked)
        {
          Properties.Settings.Default.RedOnDndCallBusy = ContactAvailability.Busy;
        }
        else
        {
          Properties.Settings.Default.RedOnDndCallBusy = ContactAvailability.None;
        }
      }

      Properties.Settings.Default.Save();
      this.Close();
    }

    private void IncomingCall_DoBlinkSingleColor_CheckedChanged(object sender, EventArgs e)
    {
      if (IncomingCall_DoBlinkSingleColor.Checked)
        IncomingCall_DoBlinkRainbow.Checked = false;
    }

    private void IncomingCall_DoBlinkRainbow_CheckedChanged(object sender, EventArgs e)
    {
      if (IncomingCall_DoBlinkRainbow.Checked)
        IncomingCall_DoBlinkSingleColor.Checked = false;
    }

    private void NewIMConversation_DoBlinkSingleColor_CheckedChanged(object sender, EventArgs e)
    {
      if (NewIMConversation_DoBlinkSingleColor.Checked)
        NewIMConversation_DoBlinkRainbow.Checked = false;
    }

    private void NewIMConversation_DoBlinkRainbow_CheckedChanged(object sender, EventArgs e)
    {
      if (NewIMConversation_DoBlinkRainbow.Checked)
        NewIMConversation_DoBlinkSingleColor.Checked = false;
    }

    private void DemoDanceButton_Click(object sender, EventArgs e)
    {
      _model.dance();
    }

    private void DemoFlapWingsButton_Click(object sender, EventArgs e)
    {
      _model.flapWings();
    }

    private void DemoGlowHeartButton_Click(object sender, EventArgs e)
    {
      _model.glowHeart();
    }

    private void DemoBlinkSingleColorButton_Click(object sender, EventArgs e)
    {
      _model.blinkSingleColor();
    }

    private void DemoBlinkMultiColorButton_Click(object sender, EventArgs e)
    {
      _model.blinkRainbow();
    }

    private void OnTrackerError_CheckedChanged(object sender, EventArgs e)
    {
      CITrackerPath.Enabled = OnTrackerError.Checked;
    }

  }
}
