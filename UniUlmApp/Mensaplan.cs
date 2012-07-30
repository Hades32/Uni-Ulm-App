using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Linq;

namespace UniUlmApp
{
    public class Mensaplan
    {
        public IList<Tag> Tage { get; private set; }
        public bool isLoaded { get; private set; }
        public IEnumerable<string> CalendarWeeks { get; private set; }

        public bool isCurrent
        {
            get
            {
                // It's a German Mensaplan :)
                var culture = new System.Globalization.CultureInfo("de-de");
                var day = DateTime.Now;
                if (day.DayOfWeek == DayOfWeek.Saturday)
                    day = day.AddDays(2);
                if (day.DayOfWeek == DayOfWeek.Sunday)
                    day = day.AddDays(1);
                var curweek = culture.Calendar.GetWeekOfYear(
                                    day,
                                    culture.DateTimeFormat.CalendarWeekRule,
                                    culture.DateTimeFormat.FirstDayOfWeek);
                return this.CalendarWeeks.Contains(curweek.ToString());
            }
        }

        public Mensaplan(System.IO.Stream stream)
        {
            this.Tage = new List<Tag>();
            this.CalendarWeeks = new string[0];
            this.parseXmlStream(stream);
        }

        private void parseXmlStream(System.IO.Stream xmlstream)
        {
            try
            {
                var plan = XDocument.Load(xmlstream);
                this.CalendarWeeks = from week in plan.Root.Elements("week")
                                     select week.Attribute("weekOfYear").Value;
                var tage = plan.Root.Elements("week").Elements("day");

                foreach (var tag in tage)
                {
                    var date = DateTime.Parse(tag.Attribute("date").Value);

                    if (tag.Attribute("open").Value == "1" && tag.Elements("meal").Count() > 0)
                    {
                        this.Tage.Add(new Tag()
                        {
                            Date = date,
                            Essen = tag.Elements("meal")
                                .Select(x => new Essen() { Typ = x.Attribute("type").Value, Name = x.Value })
                        });
                    }
                    else
                        this.Tage.Add(new Tag() { Date = date, Essen = generateNoEssenList() });
                }

                if (this.Tage.Count == 0)
                {
                    this.Tage.Add(new Tag() { Date = DateTime.Now, Essen = generateNoEssenList() });
                }

                this.isLoaded = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
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

            private DateTime _Date;

            public DateTime Date
            {
                get { return this._Date; }
                set
                {
                    if (this._Date != value)
                    {
                        this._Date = value;
                        this.PropertyChanged(this, new PropertyChangedEventArgs("Date"));
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
                return this.Date.ToShortDateString();
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
