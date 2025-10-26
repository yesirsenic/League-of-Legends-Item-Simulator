public enum RoleType { Bruiser, Tank, Assassin, Mage, Marksman, Support }

public class RoleWeight
{
    public float adDps, apDps, ehp, ttk, sustain;
}

public static class RoleWeights
{
    public static RoleWeight Get(RoleType role)
    {
        switch (role)
        {
            case RoleType.Tank: return new RoleWeight { adDps = 0.05f, apDps = 0.05f, ehp = 0.55f, ttk = 0.25f, sustain = 0.1f };
            case RoleType.Mage: return new RoleWeight { adDps = 0.25f, apDps = 0.55f, ehp = 0.05f, ttk = 0.1f, sustain = 0.05f };
            case RoleType.Marksman: return new RoleWeight { adDps = 0.5f, apDps = 0.05f, ehp = 0.1f, ttk = 0.2f, sustain = 0.15f };
            case RoleType.Assassin: return new RoleWeight { adDps = 0.4f, apDps = 0.4f, ehp = 0.05f, ttk = 0.1f, sustain = 0.05f };
            case RoleType.Support: return new RoleWeight { adDps = 0.1f, apDps = 0.2f, ehp = 0.2f, ttk = 0.2f, sustain = 0.3f };
            default: return new RoleWeight { adDps = 0.35f, apDps = 0.35f, ehp = 0.1f, ttk = 0.1f, sustain = 0.1f }; 
        }
    }
}
