using UnityEngine;

public class LambertSolver : MonoBehaviour
{
    public struct LambertResult
    {
        public bool solved;
        public string note;
        public Vector3d departureVelocity;
        public Vector3d arrivalVelocity;
    }

    private static double Sinh(double x)
    {
        return 0.5 * (Mathd.Exp(x) - Mathd.Exp(-x));
    }

    private static double Cosh(double x)
    {
        return 0.5 * (Mathd.Exp(x) + Mathd.Exp(-x));
    }

    private static double C2(double psi)
    {
        if (psi == 0.0)
            return 0.5;

        double y = Mathd.Sqrt(Mathd.Abs(psi));
        return psi > 0.0 ? ((1 - Mathd.Cos(y)) / (y * y)) : ((Cosh(y) - 1.0) / (y * y));
    }

    private static double C3(double psi)
    {
        if (psi == 0.0)
            return 1.0 / 6.0;
        
        double y = Mathd.Sqrt(Mathd.Abs(psi));
        return psi > 0.0 ? ((y - Mathd.Sin(y)) / (y * y * y)) : ((Sinh(y) - y) / (y * y * y));
    }

    public static LambertResult LambertUV(Vector3d r0, Vector3d r1, double dt, double tm, double mu, double tolerance = 1e-6, int max_steps = 200)
    {
        double psi = 0.0;
        double psi_u = 4.0 * Mathd.PI * Mathd.PI;
        double psi_l = -4.0 * Mathd.PI * Mathd.PI;

        double r0_ = r0.magnitude;
        double r1_ = r1.magnitude;
        double inv_sqrt_mu = 1.0 / Mathd.Sqrt(mu);
        double gamma = Vector3d.Dot(r0, r1) / (r0_ * r1_);
        double A = tm * Mathd.Sqrt(r0_ * r1_ * (1.0 + gamma));
        double B = 0;
        double c2 = 0.5;
        double c3 = 1.0 / 6.0;
        bool solved = false;

        LambertResult result = new LambertResult
        {
            solved = false,
            note = "No Notes",
            departureVelocity = Vector3d.zero,
            arrivalVelocity = Vector3d.zero
        };

        if (A == 0) {
            result.note = "Cant calculate orbit (A == 0)";
            return result;
        }

        for (int i = 0; i < max_steps; i++) {
            B = r0_ + r1_ + A * (psi * c3 - 1.0) / Mathd.Sqrt(c2);

            if (A > 0.0 && B < 0.0) {
                psi_l += Mathd.PI;
                B *= -1.0;
            }

            double chi3 = Mathd.Pow(Mathd.Sqrt(B / c2), 3.0);
            double dt_ = (chi3 * c3 + A * Mathd.Sqrt(B)) * inv_sqrt_mu;

            if (Mathd.Abs(dt - dt_) < tolerance) {
                solved = true;
                break;
            }

            if (dt_ <= dt) {
                psi_l = psi;
            }
            else {
                psi_u = psi;
            }

            psi = 0.5 * (psi_u + psi_l);

            c2 = C2(psi);
            c3 = C3(psi);
        }

        if (solved) {
            double f = 1.0 - B / r0_;
            double g = A * Mathd.Sqrt(B / mu);
            double gdot = 1.0 - B / r1_;

            result.solved = true;
            result.note = "Solution found";
            result.departureVelocity = (r1 - f * r0) / g;
            result.arrivalVelocity = (gdot * r1 - r0) / g;
        }
        else {
            result.note = "Cant calculate orbit (max iterations reached)";
        }

        return result;
    }
}
