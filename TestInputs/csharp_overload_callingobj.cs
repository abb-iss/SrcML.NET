namespace OverloadTest {
		public class Test {
			TestCall t;

			public void runFoo0() { t.foo(); }

			public void runFoo1() { t.foo(true); }
	}
}