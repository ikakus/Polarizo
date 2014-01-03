public class PointData
{
    public double Stokes_S0;
    public double Stokes_S1;
    public double Stokes_S2;
    public double Stokes_S3;
    public double Azimuth;
    public double DOP;
    public double Elipticity;
    public string Polariz;

    public PointData() 
    {
        Stokes_S0 = 0;
        Stokes_S1 = 0;
        Stokes_S2 = 0;
        Stokes_S3 = 0;

        Azimuth = 0;
        DOP = 0;
        Elipticity =0;
        Polariz = "";
    }
    public PointData(double s0, double s1, double s2, double s3, double az, double dop, double elip, string pol)
    {
        Stokes_S0 = s0;
        Stokes_S1 = s1;
        Stokes_S2 = s2;
        Stokes_S3 = s3;

        Azimuth = az;
        DOP = dop;
        Elipticity = elip;
        Polariz = pol; 
    }
    


};

