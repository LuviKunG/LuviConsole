using System;

namespace LuviKunG.Console
{
    [Serializable]
    public struct Extend2D
    {
        public float top;
        public float bottom;
        public float left;
        public float right;

        public float width => left + right;
        public float height => top + bottom;

        public Extend2D(float top, float bottom, float left, float right)
        {
            this.top = top;
            this.bottom = bottom;
            this.left = left;
            this.right = right;
        }

        public Extend2D(float vertical, float horizontal)
        {
            this.top = vertical;
            this.bottom = vertical;
            this.left = horizontal;
            this.right = horizontal;
        }

        public Extend2D(float all)
        {
            this.top = all;
            this.bottom = all;
            this.left = all;
            this.right = all;
        }

        public static Extend2D operator +(Extend2D a, Extend2D b)
        {
            return new Extend2D(a.top + b.top, a.bottom + b.bottom, a.left + b.left, a.right + b.right);
        }

        public static Extend2D operator -(Extend2D a, Extend2D b)
        {
            return new Extend2D(a.top - b.top, a.bottom - b.bottom, a.left - b.left, a.right - b.right);
        }

        public static Extend2D operator *(Extend2D a, Extend2D b)
        {
            return new Extend2D(a.top * b.top, a.bottom * b.bottom, a.left * b.left, a.right * b.right);
        }

        public static Extend2D operator /(Extend2D a, Extend2D b)
        {
            return new Extend2D(a.top / b.top, a.bottom / b.bottom, a.left / b.left, a.right / b.right);
        }

        public static Extend2D operator +(Extend2D a, float b)
        {
            return new Extend2D(a.top + b, a.bottom + b, a.left + b, a.right + b);
        }

        public static Extend2D operator -(Extend2D a, float b)
        {
            return new Extend2D(a.top - b, a.bottom - b, a.left - b, a.right - b);
        }

        public static Extend2D operator *(Extend2D a, float b)
        {
            return new Extend2D(a.top * b, a.bottom * b, a.left * b, a.right * b);
        }

        public static Extend2D operator /(Extend2D a, float b)
        {
            return new Extend2D(a.top / b, a.bottom / b, a.left / b, a.right / b);
        }

        public static Extend2D operator +(float a, Extend2D b)
        {
            return new Extend2D(a + b.top, a + b.bottom, a + b.left, a + b.right);
        }

        public static Extend2D operator -(float a, Extend2D b)
        {
            return new Extend2D(a - b.top, a - b.bottom, a - b.left, a - b.right);
        }

        public static Extend2D operator *(float a, Extend2D b)
        {
            return new Extend2D(a * b.top, a * b.bottom, a * b.left, a * b.right);
        }

        public static Extend2D operator /(float a, Extend2D b)
        {
            return new Extend2D(a / b.top, a / b.bottom, a / b.left, a / b.right);
        }

        public static bool operator ==(Extend2D a, Extend2D b)
        {
            return a.top == b.top && a.bottom == b.bottom && a.left == b.left && a.right == b.right;
        }

        public static bool operator !=(Extend2D a, Extend2D b)
        {
            return a.top != b.top || a.bottom != b.bottom || a.left != b.left || a.right != b.right;
        }

        public override bool Equals(object obj)
        {
            if (obj is Extend2D other)
                return top == other.top && bottom == other.bottom && left == other.left && right == other.right;
            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
