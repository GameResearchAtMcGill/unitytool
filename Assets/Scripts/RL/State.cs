//------------------------------------------------------------------------------
// <auto-generated>
//     이 코드는 도구를 사용하여 생성되었습니다.
//     런타임 버전:4.0.30319.34014
//
//     파일 내용을 변경하면 잘못된 동작이 발생할 수 있으며, 코드를 다시 생성하면
//     이러한 변경 내용이 손실됩니다.
// </auto-generated>
//------------------------------------------------------------------------------
using System;

namespace Learning
{
		public class State
		{
		
//			public	int x;
//			public int y;
			public int id;
			public double[] sensors; 
		
				public State ( int _id, double[] _inputs )
				{
//					x = _x;
//					y = _y;
					id = _id;
					sensors = _inputs;
				}
		}
}
