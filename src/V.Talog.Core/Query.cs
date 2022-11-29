using System;
using System.Collections.Generic;
using System.Text;

namespace V.Talog
{
    public class Query
    {
        public Tag Tag { get; set; }

        /// <summary>
        /// 0 eq 1 neq 2 and 3 or
        /// </summary>
        public int Type { get; set; }

        public Query Left { get; set; }

        public Query Right { get; set; }

        public Query() { }

        public Query(string label, string value)
        {
            this.Tag = new Tag
            {
                Label = label,
                Value = value
            };
        }

        public Query(Tag tag)
        {
            this.Tag = tag;
        }

        public Query(Query left, Query right, int type)
        {
            this.Left = left;
            this.Right = right;
            this.Type = type;
        }

        public Query And(Tag tag) => new Query(this, new Query(tag), 2);

        public Query And(Query query) => new Query(this, query, 2);

        public Query And(string label, string value) => new Query(this, new Query(label, value), 2);

        public Query Or(Tag tag) => new Query(this, new Query(tag), 3);

        public Query Or(Query query) => new Query(this, query, 3);

        public Query Or(string label, string value) => new Query(this, new Query(label, value), 3);

        public Query Not()
        {
            if (this.Tag != null)
            {
                if (this.Type == 0)
                {
                    this.Type = 1;
                }
                else
                {
                    this.Type = 0;
                }
            }
            else
            {
                this.Left = this.Left.Not();
                this.Right = this.Right.Not();
                if (this.Type == 2)
                {
                    this.Type = 3;
                }
                else
                {
                    this.Type = 2;
                }
            }

            return this;
        }
    }
}
