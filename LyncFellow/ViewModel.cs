using System;
using System.Collections.Generic;
using System.Text;

namespace LyncFellow
{
  public class ViewModel
  {
    Buddies _buddies;

    public ViewModel(Buddies buddies)
    {
      _buddies = buddies;
    }

    public void dance()
    {
      _buddies.Dance();
    }

    public void flapWings()
    {
      _buddies.FlapWings();
    }

    public void glowHeart()
    {
      _buddies.Heartbeat();
    }

    public void blinkSingleColor()
    {
      _buddies.BlinkSingleColor();
    }

    public void blinkRainbow()
    {
      _buddies.Rainbow();
    }
  }
}
