namespace Sooda.UnitTests.BaseObjects {
    using Sooda;
  
  
  public abstract class Vehicle : Sooda.UnitTests.BaseObjects.Stubs.Vehicle_Stub {
    
    public Vehicle(SoodaConstructor c) : 
        base(c) {
      // Do not modify this constructor.
    }
    
    public Vehicle(SoodaTransaction transaction) : 
        base(transaction) {
      // 
      // TODO: Add construction logic here.
      // 
    }
    
    public Vehicle() : 
        this(SoodaTransaction.ActiveTransaction) {
      // Do not modify this constructor.
    }
  }
}
