using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gaia
{
    /// <summary>
    /// Bounds class using double precision. Will be used to just store the min max values first, but might be extended to offer similar
    /// functionality of the regular float bounds class at some point.
    /// </summary>
    /// 
    [System.Serializable]
    public class BoundsDouble
    {
        [SerializeField]
        Vector3Double m_Center;
        [SerializeField]
        Vector3Double m_Extents;

        public Vector3Double center
        {
            get
            {
                return this.m_Center;
            }
            set
            {
                this.m_Center = value;
            }
        }

        public Vector3Double size
        {
            get
            {
                return this.m_Extents * 2f;
            }
            set
            {
                this.m_Extents = value * 0.5f;
            }
        }

        public Vector3Double extents
        {
            get
            {
                return this.m_Extents;
            }
            set
            {
                this.m_Extents = value;
            }
        }

        public Vector3Double min
        {
            get
            {
                return this.center - this.extents;
            }
            set
            {
                this.SetMinMax(value, this.max);
            }
        }

        public Vector3Double max
        {
            get
            {
                return this.center + this.extents;
            }
            set
            {
                this.SetMinMax(this.min, value);
            }
        }

        public void SetMinMax(Vector3Double min, Vector3Double max)
        {
            this.extents = (max - min) * 0.5f;
            this.center = min + this.extents;
        }


        public BoundsDouble()
        {
            this.center = Vector3Double.zero;
            this.size = Vector3Double.zero;
        }

        public static implicit operator Bounds(BoundsDouble bd)
        {
            return new Bounds(bd.center,bd.size);
        }

        public static implicit operator BoundsDouble(Bounds b)
        {
            return new BoundsDouble(b.center, b.size);
        }

        public BoundsDouble(Vector3Double center, Vector3Double size)
        {
            this.center = center;
            this.size = size;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool Intersects(Bounds bounds)
        {
            return this.min.x <= bounds.max.x && this.max.x >= bounds.min.x && (this.min.y <= bounds.max.y && this.max.y >= bounds.min.y) && this.min.z <= bounds.max.z && this.max.z >= bounds.min.z;
        }

        public bool Contains(Vector3Double point)
        {
            return min.x <= point.x && point.x <= max.x && min.y <= point.y && point.y <= max.y && min.z <= point.z && point.z <= max.z;
        }

        public void Encapsulate(Vector3 point)
        {
            this.SetMinMax(Vector3.Min(this.min, point), Vector3.Max(this.max, point));
        }

        public void Encapsulate(Bounds bounds)
        {
            this.Encapsulate(bounds.center - bounds.extents);
            this.Encapsulate(bounds.center + bounds.extents);
        }

        public void Encapsulate(Vector3Double point)
        {
            this.SetMinMax(Vector3.Min(this.min, point), Vector3.Max(this.max, point));
        }

        public void Encapsulate(BoundsDouble bounds)
        {
            this.Encapsulate(bounds.center - bounds.extents);
            this.Encapsulate(bounds.center + bounds.extents);
        }

    }
}
