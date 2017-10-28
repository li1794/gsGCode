﻿﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;
using gs;

namespace gs
{

	public class RepRapAssembler : BaseDepositionAssembler
    {
        public static BaseDepositionAssembler Factory(GCodeBuilder builder, SingleMaterialFFFSettings settings) {
            return new RepRapAssembler(builder, settings);
        }


		public SingleMaterialFFFSettings Settings;


		public RepRapAssembler(GCodeBuilder useBuilder, SingleMaterialFFFSettings settings) : base(useBuilder)
        {
			Settings = settings;
		}


		//public override void BeginRetract(Vector3d pos, double feedRate, double extrudeDist, string comment = null) {
  //          base.BeginRetract(pos, feedRate, extrudeDist, comment);
		//}

		//public override void EndRetract(Vector3d pos, double feedRate, double extrudeDist = -9999, string comment = null) {
  //          base.EndRetract(pos, feedRate, extrudeDist, comment);
		//}


        public override void UpdateProgress(int i) {
			// not supported on reprap?
			//Builder.BeginMLine(73).AppendI("P",i);
		}

		public override void ShowMessage(string s)
		{
			Builder.BeginMLine(117).AppendL(s);
		}

		public override void EnableFan() {
			// [TODO] fan speed configuration?
			Builder.BeginMLine(106, "fan on").AppendI("S", 255);
		}
		public override void DisableFan() {
			Builder.BeginMLine(107, "fan off");
		}






		public override void AppendHeader() {
			AppendHeader_StandardRepRap();
		}
		void AppendHeader_StandardRepRap() {

			Builder.AddCommentLine("; Print Settings");
			Builder.AddCommentLine("; Model: " + Settings.Machine.ManufacturerName + " " + Settings.Machine.ModelIdentifier);
			Builder.AddCommentLine("; Layer Height: " + Settings.LayerHeightMM);
			Builder.AddCommentLine("; Nozzle Diameter: " + Settings.Machine.NozzleDiamMM + "  Filament Diameter: " + Settings.Machine.FilamentDiamMM);
			Builder.AddCommentLine("; Extruder Temp: " + Settings.ExtruderTempC + " Bed Temp: " + Settings.HeatedBedTempC);

			// M109
			SetExtruderTargetTempAndWait(Settings.ExtruderTempC);

			// M190
			if (Settings.Machine.HasHeatedBed && Settings.HeatedBedTempC > 0)
				SetBedTargetTempAndWait(Settings.HeatedBedTempC);

			Builder.BeginGLine(21, "units=mm");
			Builder.BeginGLine(90, "absolute positions");
			Builder.BeginMLine(82, "absolute extruder position");

			DisableFan();

			Builder.BeginGLine(28, "home x/y").AppendI("X", 0).AppendI("Y", 0);
			currentPos.x = currentPos.y = 0;
			PositionShift = Settings.Machine.BedSizeMM * 0.5;
				
			Builder.BeginGLine(28, "home z").AppendI("Z", 0);
			currentPos.z = 0;


            double PrimeHeight = 0.27;
            double PrimeExtrudePerMM_1p75 = 0.1;
            double PrimeFeedRate = 1800;
            Vector3d frontRight = new Vector3d(Settings.Machine.BedSizeMM.x / 2, -Settings.Machine.BedSizeMM.y / 2, PrimeHeight);
            frontRight.x -= 10;
            frontRight.y += 5;
            Vector3d frontLeft = frontRight; frontLeft.x = -frontRight.x;
            Builder.BeginGLine(92, "reset extruded length").AppendI("E", 0);
            AppendMoveTo(frontRight, 9000, "start prime");
            double feed_amount = PrimeExtrudePerMM_1p75 * frontRight.Distance(frontLeft);
            AppendExtrudeTo(frontLeft, PrimeFeedRate, feed_amount, "prime");


            // [RMS] this does not extrude very much and does not seem to work?
            //Builder.BeginGLine(1, "move platform down").AppendF("Z", 15).AppendI("F", 9000);
            //currentPos.z = 15;
            //Builder.BeginGLine(92, "reset extruded length").AppendI("E", 0);
            //extruderA = 0;
            //Builder.BeginGLine(1, "extrude blob").AppendI("F", 200).AppendI("E", 3);
            //Builder.BeginGLine(92, "reset extruded length again").AppendI("E", 0);
            //extruderA = 0;
            //Builder.BeginGLine(1, "reset speed").AppendI("F", 9000);

            // move to z=0
            Builder.BeginGLine(1).AppendI("Z", 0).AppendI("F", 9000);
			currentPos.z = 0;

			ShowMessage("Print Started");

			in_retract = false;
			in_travel = false;

			UpdateProgress(0);
		}





		public override void AppendFooter() {
			AppendFooter_StandardRepRap();
		}
		void AppendFooter_StandardRepRap() {

            UpdateProgress(100);

            Builder.AddCommentLine("End of print");
            ShowMessage("Done!");

            DisableFan();
            SetExtruderTargetTemp(0, "extruder off");
            SetBedTargetTemp(0, "bed off");

            BeginRetractRelativeDist(currentPos, 300, -1, "final retract");

            Vector3d zup = currentPos;
            zup.z = Math.Min(Settings.Machine.MaxHeightMM, zup.z + 50);
            AppendMoveToE(zup, 9000, ExtruderA - 5.0, "move up and retract");

            Builder.BeginGLine(28, "home x/y").AppendI("X", 0).AppendI("Y", 0);
            currentPos.x = currentPos.y = 0;

            Builder.BeginMLine(84, "turn off steppers");

			Builder.EndLine();		// need to force this
		}

	}


}