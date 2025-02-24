﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


public class PlayerOnTimeEffect : EffectOnTime
{
    public PlayerOnTimeEffect(JSONObject js) : base()
    {
        _id = (int)js.GetField(js.keys[0]).n;

        try
        {
            _effect = SpellManager.GetInstance().GetDirectEffectById((int)js.GetField(js.keys[1]).n);
        }
        catch
        {
            Logger.Error(_id + " -->this is not a directeffect");
        }
       
        _nbTurn = (int)js.GetField(js.keys[2]).n;
    }

    /// <summary>
    /// Adds a PlayerOnTimeAppliedEffect on every Character in the area of effect.
    /// </summary>
    /// <param name="hexagons">The Hexagons affected by the Effect.</param>
    /// <param name="target">The Hexagon aimed.</param>
    /// <param name="caster">The caster of the effect.</param>
    public override void ApplyEffect(List<Hexagon> hexagons, Hexagon target, Character caster)
    {
        List<Killable> killable = PlayBoardManager.GetInstance().GetKillableInArea(hexagons);
        foreach(Killable c in killable)
        {
            PlayerOnTimeAppliedEffect effect = new PlayerOnTimeAppliedEffect(_id, _effect, _nbTurn, caster);
            c.ReceiveOnTimeEffect(effect);
        }
    }
}
