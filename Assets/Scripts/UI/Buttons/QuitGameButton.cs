﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuitGameButton : UIButton
{
    public override void OnClick(Vector3 hitPoint)
    {
        base.OnClick(hitPoint);
        Application.Quit();
    }
}
