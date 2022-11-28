using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EulerData {
    static public List<string> suffixes = new List<string>() { "-px", "-py", "-pz", "-rx", "-ry", "-rz"};

    public List<float> px { get; set; }
    public List<float> py { get; set; }
    public List<float> pz { get; set; }
    public List<float> rx { get; set; }
    public List<float> ry { get; set; }
    public List<float> rz { get; set; }

    public EulerData() {
        px = new List<float>();
        py = new List<float>();
        pz = new List<float>();
        rx = new List<float>();
        ry = new List<float>();
        rz = new List<float>();
    }

}
