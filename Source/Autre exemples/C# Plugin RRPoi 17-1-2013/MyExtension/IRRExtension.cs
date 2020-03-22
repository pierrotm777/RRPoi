using System;
using System.Runtime.InteropServices;

namespace RRPoi
{
	[Guid("3E1B0769-D34D-4b5e-883D-011588400012")]
    public interface IRRPoi
	{
		[DispId(1)]
		int ProcessCommand(string CMD, object frm);

		[DispId(2)]
		string ReturnLabel(string LBL, string FMT);

		[DispId(3)]
		string ReturnIndicator(string IND);

		[DispId(4)]
		string ReturnIndicatorEx(string IND);

		[DispId(5)]
		long ReturnSlider(string IND);

		[DispId(6)]
		void Enabled(bool status);

		[DispId(7)]
		string Properties(string item);

		[DispId(8)]
		void Initialize(string pluginDataPath);

		[DispId(9)]
        void Terminate();
//        void Unload();
	}

}
