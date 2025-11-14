using AK;

namespace WaveSurvival.CustomWaveData.Wave
{
    public enum ScreamType : byte
    {
        Striker,
        Shooter,
        Birther,
        Shadow,
        Tank,
        Flyer,
        Immortal,
        Bullrush,
        Pouncer,
        Striker_Berserk,
        Shooter_Spread,
        None
    }

    public enum ScreamSize : byte
    {
        Small,
        Medium,
        Big
    }

    public static class ScreamExtensions
    {
        public static uint ToSwitch(this ScreamType screamType)
        {
            return screamType switch
            {
                ScreamType.Striker => SWITCHES.ENEMY_TYPE.SWITCH.STRIKER,
                ScreamType.Shooter => SWITCHES.ENEMY_TYPE.SWITCH.SHOOTER,
                ScreamType.Birther => SWITCHES.ENEMY_TYPE.SWITCH.BIRTHER,
                ScreamType.Shadow => SWITCHES.ENEMY_TYPE.SWITCH.SHADOW,
                ScreamType.Tank => SWITCHES.ENEMY_TYPE.SWITCH.TANK,
                ScreamType.Flyer => SWITCHES.ENEMY_TYPE.SWITCH.FLYER,
                ScreamType.Immortal => SWITCHES.ENEMY_TYPE.SWITCH.IMMORTAL,
                ScreamType.Bullrush => SWITCHES.ENEMY_TYPE.SWITCH.BULLRUSHER,
                ScreamType.Pouncer => SWITCHES.ENEMY_TYPE.SWITCH.POUNCER,
                ScreamType.Striker_Berserk => SWITCHES.ENEMY_TYPE.SWITCH.STRIKER_BERSERK,
                ScreamType.Shooter_Spread => SWITCHES.ENEMY_TYPE.SWITCH.SHOOTER_SPREAD,
                _ => 0
            };
        }

        public static uint ToSwitch(this ScreamSize screamSize)
        {
            return screamSize switch
            {
                ScreamSize.Small => SWITCHES.ROAR_SIZE.SWITCH.SMALL,
                ScreamSize.Medium => SWITCHES.ROAR_SIZE.SWITCH.MEDIUM,
                ScreamSize.Big => SWITCHES.ROAR_SIZE.SWITCH.BIG,
                _ => 0
            };
        }
    }
}
