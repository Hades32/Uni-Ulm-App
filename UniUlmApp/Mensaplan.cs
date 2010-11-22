using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Windows;
using System.Xml.Linq;

namespace UniUlmApp
{


    public class Mensaplan
    {
        public IList<Tag> Tage { get; private set; }

        public event Action<Mensaplan> Loaded;

        public Mensaplan(string url)
        {
            this.Loaded += (_) => { };
            this.Tage = new List<Tag>();
            var web = new WebClient();
            web.OpenReadCompleted += new OpenReadCompletedEventHandler(web_OpenReadCompleted);
            web.OpenReadAsync(new Uri(url));
        }

        void web_OpenReadCompleted(object sender, OpenReadCompletedEventArgs e)
        {
            try
            {
                if (e.Error == null)
                {
                    var plan = XDocument.Load(e.Result);

                    var tage = plan.Root.Elements("week").Elements("day");

                    foreach (var tag in tage)
                    {
                        var date = formatDate(tag.Attribute("date").Value);

                        if (tag.Attribute("open").Value == "1")
                        {
                            this.Tage.Add(new Tag()
                            {
                                DateName = date,
                                FullDate = formatDateLong(tag.Attribute("date").Value),
                                Essen = tag.Elements("meal")
                                    .Select(x => new Essen() { Typ = x.Attribute("type").Value, Name = x.Value })
                            });
                        }
                        else
                            this.Tage.Add(new Tag() { DateName = date, Essen = generateNoEssenList() });
                    }
                    this.Loaded(this);
                }
                else
                {
                    MessageBox.Show("Es gab ein Problem. Versuch es später nochmal!");
                }
            }
            catch
            {
                MessageBox.Show("Es gab ein Problem. Versuch es später nochmal!");
            }
        }

        private static string formatDate(string date)
        {
            var heutedate = DateTime.Now.Year + "-" + DateTime.Now.Month + "-" + DateTime.Now.Day;
            var morgendt = DateTime.Now.AddDays(1);
            var morgendate = morgendt.Year + "-" + morgendt.Month + "-" + morgendt.Day;

            if (date == heutedate)
                date = "Heute";
            else if (date == morgendate)
                date = "Morgen";
            else
            {
                var dt = DateTime.Parse(date);
                date = dt.ToString("dddd");
            }

            return date;
        }

        private static string formatDateLong(string date)
        {
            var dt = DateTime.Parse(date);

            return dt.ToShortDateString();
        }

        private IEnumerable<Essen> generateNoEssenList()
        {
            return new List<Essen>() { new Essen() { Typ = "Heute ist leider", Name = "Geschlossen" } };
        }

        public class Tag : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            public Tag()
            {
                //avoid null checks
                this.PropertyChanged += (_, __) => { };
            }

            private string _Name;

            public string DateName
            {
                get { return this._Name; }
                set
                {
                    if (this._Name != value)
                    {
                        this._Name = value;
                        this.PropertyChanged(this, new PropertyChangedEventArgs("DateName"));
                    }
                }
            }

            private string _FullDate;

            public string FullDate
            {
                get { return this._FullDate; }
                set
                {
                    if (this._FullDate != value)
                    {
                        this._FullDate = value;
                        this.PropertyChanged(this, new PropertyChangedEventArgs("DateName"));
                    }
                }
            }

            private IEnumerable<Essen> _Essen;

            public IEnumerable<Essen> Essen
            {
                get { return this._Essen; }
                set
                {
                    if (this._Essen != value)
                    {
                        this._Essen = value;
                        this.PropertyChanged(this, new PropertyChangedEventArgs("Essen"));
                    }
                }
            }

            public override string ToString()
            {
                return this.DateName;
            }
        }

        public class Essen : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            public Essen()
            {
                //avoid null checks
                this.PropertyChanged += (_, __) => { };
            }

            private string _Name;

            public string Name
            {
                get { return this._Name; }
                set
                {
                    if (this._Name != value)
                    {
                        this._Name = value;
                        this.PropertyChanged(this, new PropertyChangedEventArgs("Name"));
                    }
                }
            }

            private string _Typ;

            public string Typ
            {
                get { return this._Typ; }
                set
                {
                    if (this._Typ != value)
                    {
                        this._Typ = value;
                        this.PropertyChanged(this, new PropertyChangedEventArgs("Typ"));
                    }
                }
            }

        }
    }
}
