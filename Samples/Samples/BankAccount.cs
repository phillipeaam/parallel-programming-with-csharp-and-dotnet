namespace Samples
{
    public class BankAccount
    {
        private readonly object _padlock = new object();
        
        public int Balance { private set; get; }

        public void Deposit(int amount)
        {
            lock (_padlock)
            {
                // This operation is not atomic
                // It's as follows:
                // (Operation 1)    temp <- Balance + amount
                // (Operation 2)    Balance <- temp
                Balance += amount; 
            }
        }

        public void Withdraw(int amount)
        {
            lock (_padlock)
            {
                // This operation is not atomic
                // It's as follows:
                // (Operation 1)    temp <- Balance - amount
                // (Operation 2)    Balance <- temp
                Balance -= amount;
            }
        }
    }
}