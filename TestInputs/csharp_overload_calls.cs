namespace OverloadTest {
	public class TestCall {
		public void CallFooWithoutParameters() {
			foo();
		}

		public void CallFooWithParameter() {
			foo(false);
		}

		public void foo() { }

		public void foo(bool bar) { }
	}
}