using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FT_ADDON.Base.Items
{
    abstract class Item<T>
    {
        public SAPbouiCOM.Form _parent;
        public SAPbouiCOM.Form parent { get => _parent; }

        private SAPbouiCOM.Item _item;
        public SAPbouiCOM.Item item { get => _item; }
        public T source { get => (T)item.Specific; }
        protected Item(string name, SAPbouiCOM.Form form, SAPbouiCOM.BoFormItemTypes itemtype)
        {
            _parent = form;

            try
            {
                _item = parent.Items.Item(name);
            }
            catch (Exception)
            {
                _item = parent.Items.Add(name, itemtype);
            }
        }

        public string Id { get => item.UniqueID; }

        public int Width
        {
            get => item.Width;
            set => item.Width = value;
        }

        public int Height
        {
            get => item.Height;
            set => item.Height = value;
        }

        public int X
        {
            get => item.Left;
            set => item.Left = value;
        }

        public int Y
        {
            get => item.Top;
            set => item.Top = value;
        }

        public bool Enabled
        {
            get => item.Enabled;
            set => item.Enabled = value;
        }
        
        public bool Visible
        {
            get => item.Visible;
            set => item.Visible = value;
        }

        public void LeftOf<C>(Item<C> item, int margin = 0)
        {
            Y = item.Y;
            X = item.X - Width - margin;
            this.item.LinkTo = item.Id;
        }

        public void RightOf<C>(Item<C> item, int margin = 0)
        {
            Y = item.Y;
            X = item.X + item.Width + margin;
            this.item.LinkTo = item.Id;
        }

        public void AboveOf<C>(Item<C> item, int margin = 0)
        {
            X = item.X;
            Y = item.Y - Height - margin;
            this.item.LinkTo = item.Id;
        }

        public void BelowOf<C>(Item<C> item, int margin = 0)
        {
            X = item.X;
            Y = item.Y + item.Height + margin;
            this.item.LinkTo = item.Id;
        }

        public void SameSizeAs<C>(Item<C> item)
        {
            Width = item.Width;
            Height = item.Height;
        }

        public void GetFocus()
        {
            parent.ActiveItem = Id;
        }
    }
}
