using System.Collections.Generic;
using Townscape.Runtime.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace Townscape.Runtime.Security
{
    /// <summary>
    /// A small light on the alarm's hardware (a sensor's LED, a keypad's lights, the bell box's
    /// strobe): a tiny box just in front of the dark one in the building's mesh, with a material
    /// of its own, so each one lights on its own.
    /// </summary>
    internal sealed class AlarmGlow
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private readonly Material _material;
        private Color _shown = new Color(-1f, -1f, -1f);

        public AlarmGlow(string name, Transform parent, HideFlags hideFlags, Material template, Color unlit, Vector3 position, Vector3 facing, Vector3 size, ICollection<Object> owned)
        {
            var glow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            glow.name = name;
            glow.hideFlags = hideFlags;
            glow.transform.SetParent(parent, false);
            glow.transform.SetPositionAndRotation(position, Quaternion.LookRotation(facing, Vector3.up));
            glow.transform.localScale = size;
            Object.Destroy(glow.GetComponent<Collider>());

            _material = new Material(template) { name = name, hideFlags = HideFlags.DontSave };
            _material.SetColor(BaseColorId, unlit);
            _material.EnableKeyword("_EMISSION");
            _material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            owned.Add(_material);

            var renderer = glow.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = _material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            Set(Color.black);
        }

        /// <summary>Lights it (a linear HDR colour; black is off).</summary>
        public void Set(Color linear)
        {
            if (linear == _shown)
            {
                return;
            }

            _shown = linear;
            _material.SetVector(EmissionColorId, new Vector4(linear.r, linear.g, linear.b, 1f));
        }
    }
}
