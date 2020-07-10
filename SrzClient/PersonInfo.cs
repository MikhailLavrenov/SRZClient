using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SrzClient
{
	public class PersonInfo
	{
		public long? _P_ID { get; set; }

		public int? _VPOLIS { get; set; }

		public string _SPOLIS { get; set; }

		public string _NPOLIS { get; set; }

		public string _ST_OKATO { get; set; }

		public string _SMO { get; set; }

		public string _SMO_OGRN { get; set; }

		public string _SMO_OK { get; set; }

		public string _SMO_NAM { get; set; }

		public string _SOC { get; set; }

		public string _NOVOR { get; set; }

		public int? _VNOV_D { get; set; }

		public string _FAM { get; set; }

		public string _IM { get; set; }

		public string _OT { get; set; }

		public int? _W { get; set; }

		public DateTime? _DR { get; set; }

		public string _DOST { get; set; }

		public string _TEL { get; set; }

		public string _FAM_P { get; set; }

		public string _IM_P { get; set; }

		public string _OT_P { get; set; }

		public int? _W_P { get; set; }

		public DateTime? _DR_P { get; set; }

		public string _DOST_P { get; set; }

		public string _MR { get; set; } //Место рождения

		public string _MPR { get; set; } //Место проживания текcт

		public string _DOCTYPE { get; set; }

		public string _DOCSER { get; set; }

		public string _DOCNUM { get; set; }

		public DateTime? _DOCDATE { get; set; }

		public string _SNILS { get; set; }

		public string _WORK { get; set; }

		public string _OKATOG { get; set; }

		public string _OKATOP { get; set; }

		public string _COMENTP { get; set; }

		public string _NHISTORY { get; set; }

		public string _KLADR { get; set; }

		public string _DOCORG { get; set; }

		public PersonAdress _ADRESS { get; set; }

		public override string ToString()
		{
			return this.PropertiesToString(Environment.NewLine);
		}
	}

	public class PersonAdress
	{
		public long? _P_ID { get; set; }

		public string _RGN { get; set; }

		public string _KRAY { get; set; }

		public string _RN { get; set; }

		public string _TOWN { get; set; }

		public string _CITY { get; set; }

		public string _STREET { get; set; }

		public string _DOM { get; set; }

		public string _KOR { get; set; }

		public string _STR { get; set; }

		public string _KV { get; set; }

		public override string ToString()
		{
			return this.PropertiesToString(Environment.NewLine);
		}
	}
}
