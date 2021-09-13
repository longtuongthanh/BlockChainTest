using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Linq;
using System.Text;

namespace BlockChainTest
{
    /// <summary>
    /// All block chains start with a SummaryBlock.
    /// 
    /// When a new user checks the block chain, 
    /// they consult the existing users for the latest blocks.
    /// 
    /// If they don't have the previous blocks, they will ask for the previous
    /// blocks from the senders, up until they have the previous ones
    /// or they have 2 summary blocks.
    /// 
    /// TODO: The LOGGER machine should keep track of all blocks generated
    /// for moniter purpose. 
    /// All machines are only required to keep up to 2 Summary Blocks.
    /// The LOGGER machine is not required to keep the data of the Summary Blocks,
    /// only its hash data.
    /// The LOGGER machine's SummaryBlock don't contain any entry
    /// 
    /// TODO: The MARKETPLACE machine should have a block chain to check for
    /// valid users. The machine knows the PK and request a signature of 
    /// a random string (contains timestamp & random stuff) to verify they have
    /// the SK. Only then does it send the token data back.
    /// </summary>
    public class BlockChain
    {
        public static HashAlgorithmName HashAlgorithm;
        public static RSASignaturePadding SignaturePadding;
        // The PK of the dev. Entries with new Token is only accepted if it is from this PK.
        public static string TrustedPK;
        // Trust the content only if there are this much blocks before the head 
        // AND there are no forks.
        public const int backtrackTrustConstant = 10;
        // After this much blocks, whichever fork is longer will be chosen regardless.
        public const int backtrackForkCutConstant = 30;

        // The number of zeroes Blocks must have
        public static byte BlockZeroCriteria;

        static BlockChain()
        {
            HashAlgorithm = HashAlgorithmName.SHA256;
            SignaturePadding = RSASignaturePadding.Pss;
            BlockZeroCriteria = 30;

            // TODO: Add TrustedPK
            // TODO: load BlockByBlockNumber from file
        }

        public static long SummaryBlockNumber;
        public static Dictionary<long, List<Block>> BlockByBlockNumber;
        public static SummaryBlock Bedrock;

        public enum BlockStatus
        {
            BlockRecieved,
            BlockInvalid,
            BlockAlreadyKnown,
            BlockUnverifiable,
            BlockValid,
            BlockTooOld
        }

        /// <summary>
        /// The new block is either an Isolated branch to be merged with the tree,
        /// or a continuation of a head branch, or a duplicate, or from an era
        /// before you're tracking.
        /// </summary>
        /// <param name="data"> raw data of the block </param>
        /// <returns>Returns whether the block is valid or not.</returns>
        public static BlockStatus RecieveBlock(byte[] data) 
        {
            Block _block = Block.Create(data);
            byte[] _hash = _block.GetHash();
            string _hashkey = SerializationUtility.ByteArrayToStringBase64(_hash);

            if (_block.BlockNumber <= Bedrock.BlockNumber)
                return BlockStatus.BlockTooOld;

            // Check if block is already among the previous blocks.
            if (BlockByBlockNumber.ContainsKey(_block.BlockNumber) &&
                BlockByBlockNumber[_block.BlockNumber].Any(
                item => item.GetHash().SequenceEqual(_block.GetHash()) &&
                item.Idem() == _block.Idem()))
                return BlockStatus.BlockAlreadyKnown;

            // Validity check
            if (!_block.CheckValid(BlockZeroCriteria))
                return BlockStatus.BlockInvalid;

            // Add to database
            BlockByBlockNumber[_block.BlockNumber].Add(_block);

            // If the block doesn't have a previous block, ask the network for it.
            if (GetPrevBlock(_block) == null)
            {
                AskForBlock(_block.PreviousHash);
                return BlockStatus.BlockUnverifiable;
            }
            else
            {
                CheckValidOwnership(_block, DeleteChain);
                if (_block.BlockNumber - Bedrock.BlockNumber > backtrackTrustConstant)
                    PruneData();
            }
            // TODO: check for duplicate entry.

            return BlockStatus.BlockRecieved;
        }

        private static void PruneDataTailTowardsBedrock()
        {
            long _tail = BlockByBlockNumber.Keys.Min();
            long _cur = Bedrock.BlockNumber;

            // Prune _tail to _cur
            while (_tail < _cur)
            {
                BlockByBlockNumber.Remove(_tail);
                _tail++;
            }
        }
        private static void PruneDataBedrockTowardsHead()
        {
            long _cur = Bedrock.BlockNumber;

            List<Block> blocks = BlockByBlockNumber[_cur + 1];
            // Removes all blocks that is not connected to bedrock.
            blocks.RemoveAll(block =>
            {
                if (block == null) return true; // Safety null check
                return !block.PreviousHash.SequenceEqual(Bedrock.Hash);
            });

            // Uh oh...
            if (blocks.Count == 0)
                return;

            // Get potential chains and choose the longest
            List<List<Block>> _potentialChains = new List<List<Block>>();
            foreach (Block block in blocks)
            {
                _potentialChains.Add(GetBlockChainWithStart(block));
            }

            int maxLength = _potentialChains.Max(blocks => blocks.Count);
            Block _bestBlock = _potentialChains.First(block => block.Count == maxLength)[0];

            if ((maxLength > backtrackForkCutConstant) // If there are multiple branches
                || (maxLength > backtrackTrustConstant && blocks.Count == 1)) // If there is only one branch
            {
                if (EditSummaryBlockWithBlock(Bedrock, _bestBlock))
                    ("Error at PruneDataBedrockTowardsHead(): " +
                        "Invalid data detected.").WriteMessage();

                // Other than the best block, remove all blockchains associated.
                BlockByBlockNumber[_cur].Remove(_bestBlock);
                foreach (Block block in BlockByBlockNumber[_cur])
                    DeleteChain(block);

                BlockByBlockNumber.Remove(_cur);
                
                PruneDataBedrockTowardsHead();
            }
        }
        /// <summary>
        /// Prune Data to save on memory
        /// </summary>
        public static void PruneData()
        {
            PruneDataTailTowardsBedrock();

            PruneDataBedrockTowardsHead();
        }

        /// <summary>
        /// Edits the summary block with the entries in the block
        /// Returns false if block is invalid in ownership or block is not a continuation of summary block.
        /// </summary>
        /// <param name="summaryBlock"> the summary block to edit </param>
        /// <param name="block"> the block to use to edit the summary block </param>
        /// <returns> the block is valid to be appended </returns>
        private static bool EditSummaryBlockWithBlock(SummaryBlock summaryBlock, Block block)
        {
            if (summaryBlock.BlockNumber != block.BlockNumber - 1)
                return false;
            if (!summaryBlock.Hash.SequenceEqual(block.PreviousHash))
                return false;
            summaryBlock.BlockNumber++;
            summaryBlock.Hash = block.GetHash();

            for (int i = 0; i < block.Entries.Count; i++)
            {
                Entry _currentEntry = block.Entries[i];

                Token _token = _currentEntry.GetToken();

                string _sourceUser = SerializationUtility.
                    ByteArrayToStringBase64(_currentEntry.SourcePK);

                if (summaryBlock.Ownership.ContainsKey(_token))
                {
                    // Someone is trying to send a token they don't own
                    if (summaryBlock.Ownership[_token] != _sourceUser)
                        return false;
                    // Else they own that token.
                }
                else
                {
                    // Someone is trying to send a non-existant token
                    if (_sourceUser != TrustedPK)
                        return false;
                    // Else it's the devs sending the user the item.
                }

                string _destUser = SerializationUtility.
                    ByteArrayToStringBase64(_currentEntry.DestinationPK);

                summaryBlock.Ownership[_token] = _destUser;
            }

            return true;
        }
        
        /// <summary>
        /// Gets the Block Chain if the end is known
        /// </summary>
        /// <param name="endBlock"></param>
        /// <returns></returns>
        private static List<Block> GetBlockChainWithEnd(Block endBlock)
        {
            List<Block> result = new List<Block>();
            Block _block = endBlock;
            while (_block != null)
            {
                result.Add(_block);
                _block = GetPrevBlock(endBlock);
            }
            result.Reverse();
            return result;
        }

        /// <summary>
        /// Gets the Block Chain if the start is known
        /// </summary>
        /// <param name="startBlock"></param>
        /// <returns></returns>
        private static List<Block> GetBlockChainWithStart(Block startBlock)
        {
            List<Block> result = new List<Block>();
            Block _block = startBlock;
            while (_block != null)
            {
                result.Add(_block);
                _block = GetNextBlock(startBlock);
            }
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="endBlock"></param>
        /// <returns> The Summary Block including the endBlock. If the chain is broken (does not reach to the
        /// Bedrock Summary Block) or contain invalid ownership entries, return null. </returns>
        public static SummaryBlock GetSummaryBlock(Block endBlock)
        {
            SummaryBlock result = Bedrock.Clone();

            long _blockNum = result.BlockNumber;

            List<Block> _blocksToTarget = GetBlockChainWithEnd(endBlock);

            // Chain broken check.
            if (endBlock.BlockNumber - _blocksToTarget.Count != result.BlockNumber)
            {
                ("Broken chain. Error at" +
                    "GetSummaryBlock(Block)").WriteMessage();
                return null;
            }

            if (_blocksToTarget.Count >0 && !_blocksToTarget[0].PreviousHash.SequenceEqual(Bedrock.Hash))
            {
                ("Chain does not connect to Bedrock. Error at" +
                    "GetSummaryBlock(Block)").WriteMessage();
                return null;
            }

            // edit result
            for (int i = 0; i < _blocksToTarget.Count; i++)
            {
                Block _currentBlock = _blocksToTarget[i];
                
                if (EditSummaryBlockWithBlock(result, _currentBlock))
                {
                    ("Chain is not valid at" +
                        "GetSummaryBlock(Block)").WriteMessage();
                    return null;
                }
            }

            return result;
        }

        /// <summary>
        /// Delete the chain starting from a block till the end.
        /// </summary>
        /// <param name="startBlock"></param>
        public static void DeleteChain(Block startBlock)
        {
            Block _blockToDelete = startBlock;
            while (_blockToDelete != null)
            {
                RemoveBlock(_blockToDelete);
                _blockToDelete = GetNextBlock(_blockToDelete);
            }
        }

        /// <summary>
        /// Check if the Ownership is valid for entries of 
        /// this block and future blocks if applicable
        /// </summary>
        /// <param name="block"> the start block </param>
        /// <param name="whatToDoIfInvalid"> invokes once on the block that 
        /// has an invalid ownership entry. </param>
        /// <param name="summaryToPrevBlock"> for internal use.
        /// The summary block to summarize the ownership up to the previous block </param>
        /// <returns> Whether the chain from here to the future is all valid. </returns>
        public static BlockStatus CheckValidOwnership(Block block, 
            Action<Block> whatToDoIfInvalid = null,
            SummaryBlock summaryToPrevBlock = null
        )
        {
            if (summaryToPrevBlock == null)
                summaryToPrevBlock = GetSummaryBlock(GetPrevBlock(block));

            if (summaryToPrevBlock == null)
            {
                return BlockStatus.BlockUnverifiable;
            }

            if (!EditSummaryBlockWithBlock(summaryToPrevBlock, block))
            {
                whatToDoIfInvalid?.Invoke(block);
                return BlockStatus.BlockInvalid;
            }

            // Check future blocks which are ignored when you notice 
            Block _nextBlock = GetNextBlock(block);
            if (_nextBlock != null)
            {
                if (CheckValidOwnership(_nextBlock, whatToDoIfInvalid, summaryToPrevBlock)
                    == BlockStatus.BlockInvalid)
                    return BlockStatus.BlockInvalid;
            }

            return BlockStatus.BlockValid;
        }

        /// <summary>
        /// Ask the network for blocks.
        /// </summary>
        /// <param name="hash"> The hash of the block requested. </param>
        public static void AskForBlock(byte[] hash)
        {
            // TODO
            throw new NotImplementedException();
        }

        /// <summary>
        /// Removes the block from the list.
        /// </summary>
        /// <param name="block"></param>
        /// <returns> Whether the block is in the list. If input is null, return true. </returns>
        public static bool RemoveBlock(Block block)
        {
            if (block == null)
                return true;

            return BlockByBlockNumber[block.BlockNumber].Remove(block);
        }

        public static Block GetBlock(byte[] hash)
        {
            foreach (List<Block> list in BlockByBlockNumber.Values)
                foreach (Block item in list)
                    if (item.GetHash().SequenceEqual(hash))
                        return item;
            return null;
        }
        // O(256)
        public static Block GetPrevBlock(Block block)
        {
            if (block == null)
                return null;

            long row = block.BlockNumber - 1;

            if (!BlockByBlockNumber.ContainsKey(row))
                return null;

            return BlockByBlockNumber[row].Find(
                item => item.GetHash().SequenceEqual(block.PreviousHash));
        }
        // O(256)   
        public static Block GetNextBlock(Block block)
        {
            if (block == null)
                return null;

            long row = block.BlockNumber + 1;

            if (!BlockByBlockNumber.ContainsKey(row))
                return null;

            return BlockByBlockNumber[row].Find(
                item => item.PreviousHash.SequenceEqual(block.GetHash()));
        }
    }
}
